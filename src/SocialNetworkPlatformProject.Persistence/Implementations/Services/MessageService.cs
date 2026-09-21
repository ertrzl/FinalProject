using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Messages;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class MessageService : IMessageService
{
    private readonly IConversationRepository _conversations;
    private readonly IConversationParticipantRepository _participants;
    private readonly IMessageRepository _messages;
    private readonly IUserRepository _users;
    private readonly IRealTimeNotifier _notifier;
    private readonly IMapper _mapper;

    public MessageService(
        IConversationRepository conversations,
        IConversationParticipantRepository participants,
        IMessageRepository messages,
        IUserRepository users,
        IRealTimeNotifier notifier,
        IMapper mapper)
    {
        _conversations = conversations;
        _participants = participants;
        _messages = messages;
        _users = users;
        _notifier = notifier;
        _mapper = mapper;
    }

    public async Task<List<GetConversationDto>> GetConversationsAsync(Guid currentUserId)
    {
        var myConversationIds = await GetMyConversationIdsAsync(currentUserId);
        if (myConversationIds.Count == 0)
            return new List<GetConversationDto>();

        var otherParticipants = await _participants.GetAll(
                p => myConversationIds.Contains(p.ConversationId) && p.UserId != currentUserId,
                asNoTracking: true)
            .ToListAsync();

        var users = await _users.GetSummariesAsync(otherParticipants.Select(p => p.UserId));

        var unreadCounts = await _messages.GetAll(
                m => myConversationIds.Contains(m.ConversationId) && m.SenderId != currentUserId && !m.IsRead,
                asNoTracking: true)
            .GroupBy(m => m.ConversationId)
            .Select(g => new { ConversationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ConversationId, x => x.Count);

        var result = new List<GetConversationDto>();
        foreach (var conversationId in myConversationIds)
        {
            var other = otherParticipants.FirstOrDefault(p => p.ConversationId == conversationId);
            if (other == null || !users.TryGetValue(other.UserId, out var user))
                continue;

            var lastMessage = await _messages.GetAll(
                    filter: m => m.ConversationId == conversationId,
                    orderBy: m => m.CreatedAt,
                    isDescending: true,
                    asNoTracking: true,
                    page: 1,
                    take: 1)
                .FirstOrDefaultAsync();

            result.Add(new GetConversationDto
            {
                Id = conversationId,
                OtherUserId = user.Id,
                OtherUserName = user.FullName,
                OtherUserAvatarUrl = user.AvatarUrl,
                IsOtherUserOnline = user.IsOnline,
                LastMessageText = lastMessage?.Text,
                LastMessageAt = lastMessage?.CreatedAt,
                LastMessageIsMine = lastMessage?.SenderId == currentUserId,
                UnreadCount = unreadCounts.GetValueOrDefault(conversationId)
            });
        }

        return result.OrderByDescending(c => c.LastMessageAt).ToList();
    }

    public async Task<PagedResult<GetMessageDto>> GetMessagesAsync(Guid currentUserId, Guid conversationId, int page, int pageSize)
    {
        await EnsureParticipantAsync(currentUserId, conversationId);

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var total = await _messages.GetAll(m => m.ConversationId == conversationId).CountAsync();

        var newestFirst = await _messages.GetAll(
                filter: m => m.ConversationId == conversationId,
                orderBy: m => m.CreatedAt,
                isDescending: true,
                asNoTracking: true,
                page: page,
                take: pageSize)
            .ToListAsync();

        newestFirst.Reverse();

        return new PagedResult<GetMessageDto>
        {
            Items = MapMessages(newestFirst, currentUserId),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<GetMessageDto> SendAsync(Guid currentUserId, PostMessageDto dto)
    {
        Guid conversationId;
        Guid recipientId;

        if (dto.ConversationId.HasValue)
        {
            var participants = await _participants.GetAll(p => p.ConversationId == dto.ConversationId.Value, asNoTracking: true).ToListAsync();
            if (participants.All(p => p.UserId != currentUserId))
                throw new NotFoundException("Conversation not found.");

            conversationId = dto.ConversationId.Value;
            recipientId = participants.First(p => p.UserId != currentUserId).UserId;
        }
        else if (dto.ReceiverId.HasValue)
        {
            recipientId = dto.ReceiverId.Value;
            if (recipientId == currentUserId)
                throw new BadRequestException("You can't message yourself.");

            if (!await _users.ExistsAsync(recipientId))
                throw new NotFoundException("User not found.");

            var existingId = await FindDirectConversationAsync(currentUserId, recipientId);
            if (existingId.HasValue)
            {
                conversationId = existingId.Value;
            }
            else
            {
                var conversation = new Conversation();
                conversation.Participants.Add(new ConversationParticipant { UserId = currentUserId });
                conversation.Participants.Add(new ConversationParticipant { UserId = recipientId });
                await _conversations.AddAsync(conversation);
                conversationId = conversation.Id;
            }
        }
        else
        {
            throw new BadRequestException("Either ConversationId or ReceiverId must be provided.");
        }

        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = currentUserId,
            Text = dto.Text.Trim()
        };

        await _messages.AddAsync(message);
        await _messages.SaveChangesAsync();

        var forRecipient = _mapper.Map<GetMessageDto>(message);
        forRecipient.IsMine = false;
        await _notifier.SendMessageAsync(recipientId, forRecipient);

        var forSender = _mapper.Map<GetMessageDto>(message);
        forSender.IsMine = true;
        return forSender;
    }

    public async Task MarkConversationAsReadAsync(Guid currentUserId, Guid conversationId)
    {
        await EnsureParticipantAsync(currentUserId, conversationId);

        var unread = await _messages.GetAll(m => m.ConversationId == conversationId && m.SenderId != currentUserId && !m.IsRead).ToListAsync();
        if (unread.Count == 0)
            return;

        foreach (var message in unread)
            message.IsRead = true;

        await _messages.SaveChangesAsync();
    }

    public async Task<int> GetUnreadCountAsync(Guid currentUserId)
    {
        var myConversationIds = await GetMyConversationIdsAsync(currentUserId);
        if (myConversationIds.Count == 0)
            return 0;

        return await _messages.GetAll(m =>
                myConversationIds.Contains(m.ConversationId) && m.SenderId != currentUserId && !m.IsRead)
            .CountAsync();
    }

    private async Task<List<Guid>> GetMyConversationIdsAsync(Guid userId)
    {
        return await _participants.GetAll(p => p.UserId == userId, asNoTracking: true)
            .Select(p => p.ConversationId)
            .ToListAsync();
    }

    private async Task EnsureParticipantAsync(Guid userId, Guid conversationId)
    {
        if (!await _participants.AnyAsync(p => p.ConversationId == conversationId && p.UserId == userId))
            throw new NotFoundException("Conversation not found.");
    }

    private async Task<Guid?> FindDirectConversationAsync(Guid userA, Guid userB)
    {
        var myConversationIds = await GetMyConversationIdsAsync(userA);

        return await _participants.GetAll(p => p.UserId == userB && myConversationIds.Contains(p.ConversationId), asNoTracking: true)
            .Select(p => (Guid?)p.ConversationId)
            .FirstOrDefaultAsync();
    }

    private List<GetMessageDto> MapMessages(List<Message> messages, Guid currentUserId)
    {
        var dtos = _mapper.Map<List<GetMessageDto>>(messages);
        for (var i = 0; i < dtos.Count; i++)
            dtos[i].IsMine = messages[i].SenderId == currentUserId;

        return dtos;
    }
}
