using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.DTOs.Messages;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class MessageService : IMessageService
{
    private readonly IConversationRepository _conversations;
    private readonly IConversationParticipantRepository _participants;
    private readonly IMessageRepository _messages;
    private readonly IStoryRepository _stories;
    private readonly IUserRepository _users;
    private readonly IFriendService _friends;
    private readonly IBlockService _blocks;
    private readonly IRealTimeNotifier _notifier;
    private readonly IPresenceTracker _presence;
    private readonly IFileStorageService _files;
    private readonly IMapper _mapper;

    public MessageService(
        IConversationRepository conversations,
        IConversationParticipantRepository participants,
        IMessageRepository messages,
        IStoryRepository stories,
        IUserRepository users,
        IFriendService friends,
        IBlockService blocks,
        IRealTimeNotifier notifier,
        IPresenceTracker presence,
        IFileStorageService files,
        IMapper mapper)
    {
        _conversations = conversations;
        _participants = participants;
        _messages = messages;
        _stories = stories;
        _users = users;
        _friends = friends;
        _blocks = blocks;
        _notifier = notifier;
        _presence = presence;
        _files = files;
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
                m => myConversationIds.Contains(m.ConversationId) && m.SenderId != currentUserId && !m.IsRead && m.DeletedAt == null,
                asNoTracking: true)
            .GroupBy(m => m.ConversationId)
            .Select(g => new { ConversationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ConversationId, x => x.Count);

        var lastMessages = await _messages.GetAll(m => myConversationIds.Contains(m.ConversationId), asNoTracking: true)
            .GroupBy(m => m.ConversationId)
            .Select(g => g.OrderByDescending(m => m.CreatedAt).First())
            .ToDictionaryAsync(m => m.ConversationId);

        var otherByConversation = otherParticipants
            .GroupBy(p => p.ConversationId)
            .ToDictionary(g => g.Key, g => g.First());

        var result = new List<GetConversationDto>();
        foreach (var conversationId in myConversationIds)
        {
            if (!otherByConversation.TryGetValue(conversationId, out var other) || !users.TryGetValue(other.UserId, out var user))
                continue;

            lastMessages.TryGetValue(conversationId, out var lastMessage);

            result.Add(new GetConversationDto
            {
                Id = conversationId,
                OtherUserId = user.Id,
                OtherUserName = user.FullName,
                OtherUserAvatarUrl = user.AvatarUrl,
                IsOtherUserOnline = user.ShowOnlineStatus && _presence.IsOnline(user.Id),
                LastMessageText = lastMessage == null ? null : PreviewText(lastMessage),
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
            Items = await MapMessagesAsync(newestFirst, currentUserId),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<GetMessageDto> SendAsync(Guid currentUserId, PostMessageDto dto)
    {
        var type = dto.Type == "Sticker" ? MessageType.Sticker : MessageType.Text;
        if (dto.StoryId.HasValue && type != MessageType.Text)
            throw new BadRequestException(Loc.T("message.storyTextOnly"));

        return await SendCoreAsync(currentUserId, dto.ConversationId, dto.ReceiverId, type, dto.Text.Trim(), null, dto.StoryId);
    }

    public async Task<GetMessageDto> SendImageAsync(Guid currentUserId, PostMessageImageDto dto)
    {
        var mediaUrl = await _files.SaveImageAsync(dto.Image!, "messages");
        try
        {
            return await SendCoreAsync(currentUserId, dto.ConversationId, dto.ReceiverId, MessageType.Image, (dto.Text ?? string.Empty).Trim(), mediaUrl);
        }
        catch
        {
            // The message was never created (e.g. unknown conversation), so don't leave the upload orphaned.
            _files.Delete(mediaUrl);
            throw;
        }
    }

    private async Task<GetMessageDto> SendCoreAsync(
        Guid currentUserId, Guid? conversationIdArg, Guid? receiverIdArg, MessageType type, string text, string? mediaUrl,
        Guid? storyId = null)
    {
        Guid conversationId;
        Guid recipientId;

        if (conversationIdArg.HasValue)
        {
            var participants = await _participants.GetAll(p => p.ConversationId == conversationIdArg.Value, asNoTracking: true).ToListAsync();
            if (participants.All(p => p.UserId != currentUserId))
                throw new NotFoundException(Loc.T("conversation.notFound"));

            conversationId = conversationIdArg.Value;
            recipientId = participants.First(p => p.UserId != currentUserId).UserId;
        }
        else if (receiverIdArg.HasValue)
        {
            recipientId = receiverIdArg.Value;
            if (recipientId == currentUserId)
                throw new BadRequestException(Loc.T("message.self"));

            if (!await _users.ExistsAsync(recipientId))
                throw new NotFoundException(Loc.T("user.notFound"));

            var existingId = await _conversations.FindDirectAsync(currentUserId, recipientId);
            if (existingId.HasValue)
            {
                conversationId = existingId.Value;
            }
            else
            {
                if (!await CanStartConversationAsync(currentUserId, recipientId))
                    throw new ForbiddenException(Loc.T("message.friendsOnly"));

                conversationId = await _conversations.GetOrCreateDirectAsync(currentUserId, recipientId);
            }
        }
        else
        {
            throw new BadRequestException(Loc.T("message.target"));
        }

        // Blocked people can't write to each other, in an old conversation or a new one.
        if (await _blocks.AreBlockedAsync(currentUserId, recipientId))
            throw new ForbiddenException(Loc.T("message.cannot"));

        // An answer to a story: the story must be up, belong to the person written to, and be one the sender may see.
        if (storyId.HasValue)
        {
            var story = await _stories.GetByIdAsync(storyId.Value);
            if (story == null || story.ExpiresAt <= DateTime.UtcNow || !await _friends.AreFriendsAsync(currentUserId, story.UserId))
                throw new NotFoundException(Loc.T("story.notFound"));

            if (story.UserId != recipientId)
                throw new BadRequestException(Loc.T("message.storyOwnerOnly"));
        }

        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = currentUserId,
            Type = type,
            Text = text,
            MediaUrl = mediaUrl,
            ReplyToStoryId = storyId
        };

        await _messages.AddAsync(message);
        await _messages.SaveChangesAsync();

        var storyReply = storyId.HasValue ? (await LoadStoryRepliesAsync(new[] { storyId.Value }))[storyId.Value] : null;

        var forRecipient = _mapper.Map<GetMessageDto>(message);
        forRecipient.IsMine = false;
        forRecipient.StoryReply = storyReply;
        await _notifier.SendMessageAsync(recipientId, forRecipient);

        var forSender = _mapper.Map<GetMessageDto>(message);
        forSender.IsMine = true;
        forSender.StoryReply = storyReply;
        // Also reaches the sender's other open tabs/devices; the client de-duplicates by message id.
        await _notifier.SendMessageAsync(currentUserId, forSender);
        return forSender;
    }

    public async Task<GetMessageDto> DeleteAsync(Guid currentUserId, Guid messageId)
    {
        var message = await _messages.GetByIdAsync(messageId)
            ?? throw new NotFoundException(Loc.T("message.notFound"));

        // Somebody outside the conversation must not learn that the message exists.
        var participants = await _participants.GetAll(p => p.ConversationId == message.ConversationId, asNoTracking: true).ToListAsync();
        if (participants.All(p => p.UserId != currentUserId))
            throw new NotFoundException(Loc.T("message.notFound"));

        if (message.SenderId != currentUserId)
            throw new ForbiddenException(Loc.T("message.deleteOwn"));

        if (!message.IsDeleted)
        {
            var mediaUrl = message.MediaUrl;
            message.MarkDeleted();
            await _messages.SaveChangesAsync();

            _files.Delete(mediaUrl);

            // Every open tab of both people replaces the bubble; each gets the message as it sees it.
            foreach (var other in participants.Where(p => p.UserId != currentUserId))
            {
                var forOther = _mapper.Map<GetMessageDto>(message);
                forOther.IsMine = false;
                await _notifier.PublishToMessagesAsync(other.UserId, "MessageDeleted", forOther);
            }
        }

        var forSender = _mapper.Map<GetMessageDto>(message);
        forSender.IsMine = true;
        await _notifier.PublishToMessagesAsync(currentUserId, "MessageDeleted", forSender);
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

        // Read receipt: whoever sent those messages sees them turn "seen".
        foreach (var senderId in unread.Select(m => m.SenderId).Distinct())
            await _notifier.PublishToMessagesAsync(senderId, "MessagesRead", new { conversationId, readerId = currentUserId });
    }

    public async Task NotifyTypingAsync(Guid currentUserId, Guid conversationId)
    {
        var participants = await _participants.GetAll(p => p.ConversationId == conversationId, asNoTracking: true).ToListAsync();
        if (participants.All(p => p.UserId != currentUserId))
            throw new NotFoundException(Loc.T("conversation.notFound"));

        foreach (var other in participants.Where(p => p.UserId != currentUserId))
            await _notifier.PublishToMessagesAsync(other.UserId, "UserTyping", new { conversationId, userId = currentUserId });
    }

    public async Task<int> GetUnreadCountAsync(Guid currentUserId)
    {
        var myConversationIds = await GetMyConversationIdsAsync(currentUserId);
        if (myConversationIds.Count == 0)
            return 0;

        return await _messages.GetAll(m =>
                myConversationIds.Contains(m.ConversationId) && m.SenderId != currentUserId && !m.IsRead && m.DeletedAt == null)
            .CountAsync();
    }

    public async Task<bool> CanMessageAsync(Guid currentUserId, Guid otherUserId)
    {
        if (currentUserId == otherUserId || await _blocks.AreBlockedAsync(currentUserId, otherUserId))
            return false;

        return await _conversations.FindDirectAsync(currentUserId, otherUserId) != null
            || await CanStartConversationAsync(currentUserId, otherUserId);
    }

    // New conversations: friends, or someone whose account is open. Existing ones are never affected (see CanMessageAsync).
    private async Task<bool> CanStartConversationAsync(Guid currentUserId, Guid recipientId)
    {
        var recipient = await _users.GetSummaryAsync(recipientId);
        if (recipient is { IsPrivateAccount: false })
            return true;

        return await _friends.AreFriendsAsync(currentUserId, recipientId);
    }

    // The conversations the list and the unread counter show: the ones with a blocked person are hidden (not deleted:
    // they come back when the block is lifted).
    private async Task<List<Guid>> GetMyConversationIdsAsync(Guid userId)
    {
        var ids = await _participants.GetAll(p => p.UserId == userId, asNoTracking: true)
            .Select(p => p.ConversationId)
            .ToListAsync();

        var blockedIds = await _blocks.GetBlockedIdsAsync(userId);
        if (ids.Count == 0 || blockedIds.Count == 0)
            return ids;

        var hidden = await _participants.GetAll(p => ids.Contains(p.ConversationId) && blockedIds.Contains(p.UserId), asNoTracking: true)
            .Select(p => p.ConversationId)
            .ToListAsync();

        return ids.Except(hidden).ToList();
    }

    private async Task EnsureParticipantAsync(Guid userId, Guid conversationId)
    {
        if (!await _participants.AnyAsync(p => p.ConversationId == conversationId && p.UserId == userId))
            throw new NotFoundException(Loc.T("conversation.notFound"));
    }

    // What the conversation list shows as the last message.
    private static string PreviewText(Message message)
    {
        if (message.IsDeleted)
            return Loc.T("message.preview.deleted");

        return message.Type == MessageType.Image ? Loc.T("message.preview.photo") : message.Text;
    }

    private async Task<List<GetMessageDto>> MapMessagesAsync(List<Message> messages, Guid currentUserId)
    {
        var dtos = _mapper.Map<List<GetMessageDto>>(messages);
        var replies = await LoadStoryRepliesAsync(messages.Where(m => m.ReplyToStoryId.HasValue).Select(m => m.ReplyToStoryId!.Value));
        for (var i = 0; i < dtos.Count; i++)
        {
            dtos[i].IsMine = messages[i].SenderId == currentUserId;
            if (messages[i].ReplyToStoryId is { } storyId)
                dtos[i].StoryReply = replies[storyId];
        }

        return dtos;
    }

    // The stories some messages answer, in one query. A story that is gone or expired comes back as not available
    // (and without its media: an expired story is no longer shown to anybody).
    private async Task<Dictionary<Guid, GetStoryReplyDto>> LoadStoryRepliesAsync(IEnumerable<Guid> storyIds)
    {
        var ids = storyIds.Distinct().ToList();
        var result = ids.ToDictionary(id => id, id => new GetStoryReplyDto { StoryId = id });
        if (ids.Count == 0)
            return result;

        var now = DateTime.UtcNow;
        var stories = await _stories.GetAll(s => ids.Contains(s.Id) && s.ExpiresAt > now, asNoTracking: true).ToListAsync();
        foreach (var story in stories)
        {
            var reply = result[story.Id];
            reply.IsAvailable = true;
            reply.MediaUrl = story.MediaUrl;
            reply.MediaType = story.MediaType.ToString();
        }

        return result;
    }
}
