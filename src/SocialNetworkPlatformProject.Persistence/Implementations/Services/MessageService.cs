using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Messages;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;
using SocialNetworkPlatformProject.Persistence.Extensions;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class MessageService : IMessageService
{
    private readonly IConversationRepository _conversations;
    private readonly IConversationParticipantRepository _participants;
    private readonly IMessageRepository _messages;
    private readonly IMessageReactionRepository _reactions;
    private readonly IStoryRepository _stories;
    private readonly IUserRepository _users;
    private readonly IGroupRepository _groups;
    private readonly IGroupMemberRepository _groupMembers;
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
        IMessageReactionRepository reactions,
        IStoryRepository stories,
        IUserRepository users,
        IGroupRepository groups,
        IGroupMemberRepository groupMembers,
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
        _reactions = reactions;
        _stories = stories;
        _users = users;
        _groups = groups;
        _groupMembers = groupMembers;
        _friends = friends;
        _blocks = blocks;
        _notifier = notifier;
        _presence = presence;
        _files = files;
        _mapper = mapper;
    }

    public async Task<List<GetConversationDto>> GetConversationsAsync(Guid currentUserId)
    {
        await _conversations.SyncGroupChatsAsync(currentUserId);

        var myConversationIds = await GetMyConversationIdsAsync(currentUserId);
        var result = await BuildConversationsAsync(currentUserId, myConversationIds);
        return result.OrderByDescending(c => c.LastMessageAt).ToList();
    }

    private async Task<List<GetConversationDto>> BuildConversationsAsync(Guid currentUserId, List<Guid> conversationIds)
    {
        if (conversationIds.Count == 0)
            return new List<GetConversationDto>();

        var groupOf = await _conversations.GetGroupIdsAsync(conversationIds);
        var groupChatSet = await _conversations.GetGroupChatIdsAsync(conversationIds);
        var directIds = conversationIds.Where(id => !groupChatSet.Contains(id)).ToList();
        var groupChatIds = conversationIds.Where(groupChatSet.Contains).ToList();
        var customIds = groupChatIds.Where(id => !groupOf.ContainsKey(id)).ToList();
        var hidden = await _blocks.GetBlockedIdsAsync(currentUserId);

        var customChats = customIds.Count == 0
            ? new Dictionary<Guid, Conversation>()
            : await _conversations.GetAll(c => customIds.Contains(c.Id), asNoTracking: true).ToDictionaryAsync(c => c.Id);
        var customMembers = customIds.Count == 0
            ? new Dictionary<Guid, List<ConversationParticipant>>()
            : (await _participants.GetAll(p => customIds.Contains(p.ConversationId), asNoTracking: true).ToListAsync())
                .GroupBy(p => p.ConversationId)
                .ToDictionary(g => g.Key, g => g.ToList());

        var otherParticipants = await _participants.GetAll(
                p => directIds.Contains(p.ConversationId) && p.UserId != currentUserId,
                asNoTracking: true)
            .ToListAsync();

        var directUnread = await _messages.GetAll(
                m => directIds.Contains(m.ConversationId) && m.SenderId != currentUserId && !m.IsRead && m.DeletedAt == null,
                asNoTracking: true)
            .GroupBy(m => m.ConversationId)
            .Select(g => new { ConversationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ConversationId, x => x.Count);

        var groupUnread = await _messages.CountGroupUnreadAsync(currentUserId, groupChatIds, hidden);

        var lastMessages = await _messages.GetAll(
                m => conversationIds.Contains(m.ConversationId) && !(groupChatIds.Contains(m.ConversationId) && hidden.Contains(m.SenderId)),
                asNoTracking: true)
            .GroupBy(m => m.ConversationId)
            .Select(g => g.OrderByDescending(m => m.CreatedAt).First())
            .ToDictionaryAsync(m => m.ConversationId);

        var groupIds = groupOf.Values.Distinct().ToList();
        var groups = groupIds.Count == 0
            ? new Dictionary<Guid, Group>()
            : await _groups.GetAll(g => groupIds.Contains(g.Id), asNoTracking: true).ToDictionaryAsync(g => g.Id);
        var memberships = groupIds.Count == 0
            ? new Dictionary<Guid, GroupMembership>()
            : await _groups.GetMembershipsAsync(groupIds, currentUserId);
        var mutedGroupIds = await MutedGroupIdsAsync(currentUserId, groupIds);

        var groupLastSenders = groupChatIds.Where(lastMessages.ContainsKey).Select(id => lastMessages[id].SenderId);
        var users = await _users.GetSummariesAsync(otherParticipants.Select(p => p.UserId).Concat(groupLastSenders));

        var otherByConversation = otherParticipants
            .GroupBy(p => p.ConversationId)
            .ToDictionary(g => g.Key, g => g.First());

        var result = new List<GetConversationDto>();
        foreach (var conversationId in conversationIds)
        {
            lastMessages.TryGetValue(conversationId, out var lastMessage);

            if (customChats.TryGetValue(conversationId, out var custom))
            {
                var members = customMembers.GetValueOrDefault(conversationId) ?? new List<ConversationParticipant>();
                var mine = members.FirstOrDefault(p => p.UserId == currentUserId);
                result.Add(new GetConversationDto
                {
                    Id = conversationId,
                    IsGroup = true,
                    IsCustomGroup = true,
                    CanManage = mine?.IsAdmin == true,
                    MemberCount = members.Count,
                    IsMuted = mine?.IsMuted == true,
                    OtherUserId = Guid.Empty,
                    OtherUserName = custom.Title!,
                    OtherUserAvatarUrl = custom.AvatarUrl,
                    IsOtherUserOnline = false,
                    LastMessageText = lastMessage == null ? null : PreviewText(lastMessage),
                    LastMessageAt = lastMessage?.CreatedAt ?? custom.CreatedAt,
                    LastMessageIsMine = lastMessage?.SenderId == currentUserId,
                    LastMessageSenderName = lastMessage != null && users.TryGetValue(lastMessage.SenderId, out var customSender) ? customSender.FullName : null,
                    UnreadCount = groupUnread.GetValueOrDefault(conversationId)
                });
                continue;
            }

            if (groupOf.TryGetValue(conversationId, out var groupId))
            {
                if (!groups.TryGetValue(groupId, out var group))
                    continue;

                result.Add(new GetConversationDto
                {
                    Id = conversationId,
                    GroupId = groupId,
                    IsGroup = true,
                    MemberCount = memberships.GetValueOrDefault(groupId)?.MemberCount ?? 0,
                    IsMuted = mutedGroupIds.Contains(groupId),
                    OtherUserId = Guid.Empty,
                    OtherUserName = group.Name,
                    OtherUserAvatarUrl = group.AvatarUrl,
                    IsOtherUserOnline = false,
                    LastMessageText = lastMessage == null ? null : PreviewText(lastMessage),
                    LastMessageAt = lastMessage?.CreatedAt,
                    LastMessageIsMine = lastMessage?.SenderId == currentUserId,
                    LastMessageSenderName = lastMessage != null && users.TryGetValue(lastMessage.SenderId, out var sender) ? sender.FullName : null,
                    UnreadCount = groupUnread.GetValueOrDefault(conversationId)
                });
                continue;
            }

            if (!otherByConversation.TryGetValue(conversationId, out var other) || !users.TryGetValue(other.UserId, out var user))
                continue;

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
                UnreadCount = directUnread.GetValueOrDefault(conversationId)
            });
        }

        return result;
    }

    public async Task<PagedResult<GetMessageDto>> GetMessagesAsync(Guid currentUserId, Guid conversationId, int page, int pageSize)
    {
        await EnsureParticipantAsync(currentUserId, conversationId);

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var hidden = await IsGroupChatAsync(conversationId)
            ? await _blocks.GetBlockedIdsAsync(currentUserId)
            : new HashSet<Guid>();

        var total = await _messages.GetAll(m => m.ConversationId == conversationId && !hidden.Contains(m.SenderId)).CountAsync();

        var newestFirst = await _messages.GetAll(
                filter: m => m.ConversationId == conversationId && !hidden.Contains(m.SenderId),
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

        return await SendCoreAsync(currentUserId, dto.ConversationId, dto.ReceiverId, type, dto.Text.Trim(), null, dto.StoryId, dto.ReplyToMessageId);
    }

    public async Task<GetMessageDto> SendImageAsync(Guid currentUserId, PostMessageImageDto dto)
    {
        var mediaUrl = await _files.SaveImageAsync(dto.Image!, "messages");
        try
        {
            return await SendCoreAsync(currentUserId, dto.ConversationId, dto.ReceiverId, MessageType.Image, (dto.Text ?? string.Empty).Trim(), mediaUrl,
                replyToMessageId: dto.ReplyToMessageId);
        }
        catch
        {
            _files.Delete(mediaUrl);
            throw;
        }
    }

    private async Task<GetMessageDto> SendCoreAsync(
        Guid currentUserId, Guid? conversationIdArg, Guid? receiverIdArg, MessageType type, string text, string? mediaUrl,
        Guid? storyId = null, Guid? replyToMessageId = null)
    {
        Guid conversationId;
        List<Guid> recipientIds;
        var isGroup = false;

        if (conversationIdArg.HasValue)
        {
            var participants = await _participants.GetAll(p => p.ConversationId == conversationIdArg.Value, asNoTracking: true).ToListAsync();
            if (participants.All(p => p.UserId != currentUserId))
                throw new NotFoundException(Loc.T("conversation.notFound"));

            conversationId = conversationIdArg.Value;
            isGroup = await IsGroupChatAsync(conversationId);
            recipientIds = participants.Where(p => p.UserId != currentUserId).Select(p => p.UserId).ToList();

            if (!isGroup && recipientIds.Count == 0)
                throw new NotFoundException(Loc.T("conversation.notFound"));
        }
        else if (receiverIdArg.HasValue)
        {
            var recipientId = receiverIdArg.Value;
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

            recipientIds = new List<Guid> { recipientId };
        }
        else
        {
            throw new BadRequestException(Loc.T("message.target"));
        }

        var hidden = await _blocks.GetBlockedIdsAsync(currentUserId);
        if (!isGroup && hidden.Contains(recipientIds[0]))
            throw new ForbiddenException(Loc.T("message.cannot"));

        if (storyId.HasValue)
        {
            if (isGroup)
                throw new BadRequestException(Loc.T("message.storyGroup"));

            var story = await _stories.GetByIdAsync(storyId.Value);
            if (story == null || story.ExpiresAt <= DateTime.UtcNow || !await _friends.AreFriendsAsync(currentUserId, story.UserId))
                throw new NotFoundException(Loc.T("story.notFound"));

            if (story.UserId != recipientIds[0])
                throw new BadRequestException(Loc.T("message.storyOwnerOnly"));
        }

        if (replyToMessageId.HasValue)
        {
            var quoted = await _messages.GetByIdAsync(replyToMessageId.Value);
            if (quoted == null || quoted.ConversationId != conversationId || quoted.IsDeleted || (isGroup && hidden.Contains(quoted.SenderId)))
                throw new NotFoundException(Loc.T("message.replyInvalid"));
        }

        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = currentUserId,
            Type = type,
            Text = text,
            MediaUrl = mediaUrl,
            ReplyToStoryId = storyId,
            ReplyToMessageId = replyToMessageId
        };

        await _messages.AddAsync(message);
        await _messages.SaveChangesAsync();

        var viewers = recipientIds.Where(id => !hidden.Contains(id)).Append(currentUserId).ToList();
        var forViewers = await MapForViewersAsync(message, viewers);
        foreach (var recipientId in viewers.Where(id => id != currentUserId))
            await _notifier.SendMessageAsync(recipientId, forViewers[recipientId]);

        var forSender = forViewers[currentUserId];
        await _notifier.SendMessageAsync(currentUserId, forSender);
        return forSender;
    }

    public async Task<GetMessageDto> DeleteAsync(Guid currentUserId, Guid messageId)
    {
        var message = await _messages.GetByIdAsync(messageId)
            ?? throw new NotFoundException(Loc.T("message.notFound"));

        var participants = await _participants.GetAll(p => p.ConversationId == message.ConversationId, asNoTracking: true).ToListAsync();
        if (participants.All(p => p.UserId != currentUserId))
            throw new NotFoundException(Loc.T("message.notFound"));

        if (message.SenderId != currentUserId)
            throw new ForbiddenException(Loc.T("message.deleteOwn"));

        var others = new List<Guid>();
        if (!message.IsDeleted)
        {
            var mediaUrl = message.MediaUrl;
            message.MarkDeleted();
            await _messages.SaveChangesAsync();
            await _reactions.DeleteForMessageAsync(messageId);

            _files.Delete(mediaUrl);
            others = await VisibleToOthersAsync(message, participants, currentUserId);
        }

        var forViewers = await MapForViewersAsync(message, others.Append(currentUserId).ToList());
        foreach (var other in others)
            await _notifier.PublishToMessagesAsync(other, "MessageDeleted", forViewers[other]);

        var forSender = forViewers[currentUserId];
        await _notifier.PublishToMessagesAsync(currentUserId, "MessageDeleted", forSender);
        return forSender;
    }

    public async Task<GetMessageDto> SetReactionAsync(Guid currentUserId, Guid messageId, string? emoji)
    {
        var message = await _messages.GetByIdAsync(messageId)
            ?? throw new NotFoundException(Loc.T("message.notFound"));

        var participants = await _participants.GetAll(p => p.ConversationId == message.ConversationId, asNoTracking: true).ToListAsync();
        if (participants.All(p => p.UserId != currentUserId))
            throw new NotFoundException(Loc.T("message.notFound"));

        if (message.IsDeleted)
            throw new BadRequestException(Loc.T("message.reactDeleted"));

        if (await IsGroupChatAsync(message.ConversationId))
        {
            if (message.SenderId != currentUserId && await _blocks.AreBlockedAsync(currentUserId, message.SenderId))
                throw new NotFoundException(Loc.T("message.notFound"));
        }
        else
        {
            foreach (var other in participants.Where(p => p.UserId != currentUserId))
            {
                if (await _blocks.AreBlockedAsync(currentUserId, other.UserId))
                    throw new ForbiddenException(Loc.T("message.cannot"));
            }
        }

        emoji = string.IsNullOrWhiteSpace(emoji) ? null : emoji.Trim();
        if (emoji != null && !MessageReactions.Allowed.Contains(emoji))
            throw new BadRequestException(Loc.T("message.reactionInvalid"));

        var existing = await _reactions.GetAll(r => r.MessageId == messageId && r.UserId == currentUserId).FirstOrDefaultAsync();
        if (existing != null && (emoji == null || existing.Emoji == emoji))
            _reactions.Delete(existing);
        else if (existing != null)
            existing.Emoji = emoji!;
        else if (emoji != null)
            await _reactions.AddAsync(new MessageReaction { MessageId = messageId, UserId = currentUserId, Emoji = emoji });

        try
        {
            await _reactions.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
        }

        var viewers = (await VisibleToOthersAsync(message, participants, currentUserId)).Append(currentUserId).ToList();
        var forViewers = await MapForViewersAsync(message, viewers);
        foreach (var viewer in viewers)
            await _notifier.PublishToMessagesAsync(viewer, "MessageUpdated", forViewers[viewer]);

        return forViewers[currentUserId];
    }

    public async Task MarkConversationAsReadAsync(Guid currentUserId, Guid conversationId)
    {
        await EnsureParticipantAsync(currentUserId, conversationId);

        if (await IsGroupChatAsync(conversationId))
        {
            await MarkGroupChatAsReadAsync(currentUserId, conversationId);
            return;
        }

        var unread = await _messages.GetAll(m => m.ConversationId == conversationId && m.SenderId != currentUserId && !m.IsRead).ToListAsync();
        if (unread.Count == 0)
            return;

        foreach (var message in unread)
            message.IsRead = true;

        await _messages.SaveChangesAsync();

        foreach (var senderId in unread.Select(m => m.SenderId).Distinct())
            await _notifier.PublishToMessagesAsync(senderId, "MessagesRead", new { conversationId, readerId = currentUserId });
    }

    private async Task MarkGroupChatAsReadAsync(Guid currentUserId, Guid conversationId)
    {
        var me = await _participants.GetAll(p => p.ConversationId == conversationId && p.UserId == currentUserId).FirstAsync();
        var since = me.ReadUpTo;

        var senderIds = await _messages.GetAll(
                m => m.ConversationId == conversationId && m.SenderId != currentUserId && m.DeletedAt == null && m.CreatedAt > since,
                asNoTracking: true)
            .Select(m => m.SenderId)
            .Distinct()
            .ToListAsync();
        if (senderIds.Count == 0)
            return;

        me.LastReadAt = DateTime.UtcNow;
        await _participants.SaveChangesAsync();

        var hidden = await _blocks.GetBlockedIdsAsync(currentUserId);
        var everyone = await _participants.GetAll(p => p.ConversationId == conversationId, asNoTracking: true).ToListAsync();
        foreach (var senderId in senderIds.Where(id => !hidden.Contains(id) && everyone.Any(p => p.UserId == id)))
            await _notifier.PublishToMessagesAsync(senderId, "MessagesRead",
                new { conversationId, readerId = currentUserId, readUpTo = ReadByAllOthers(everyone, senderId) });
    }

    public async Task NotifyTypingAsync(Guid currentUserId, Guid conversationId)
    {
        var participants = await _participants.GetAll(p => p.ConversationId == conversationId, asNoTracking: true).ToListAsync();
        if (participants.All(p => p.UserId != currentUserId))
            throw new NotFoundException(Loc.T("conversation.notFound"));

        if (!await IsGroupChatAsync(conversationId))
        {
            foreach (var other in participants.Where(p => p.UserId != currentUserId))
                await _notifier.PublishToMessagesAsync(other.UserId, "UserTyping", new { conversationId, userId = currentUserId });
            return;
        }

        var hidden = await _blocks.GetBlockedIdsAsync(currentUserId);
        var userName = (await _users.GetSummaryAsync(currentUserId))?.FullName ?? string.Empty;
        foreach (var other in participants.Where(p => p.UserId != currentUserId && !hidden.Contains(p.UserId)))
            await _notifier.PublishToMessagesAsync(other.UserId, "UserTyping", new { conversationId, userId = currentUserId, userName });
    }

    public async Task<int> GetUnreadCountAsync(Guid currentUserId)
    {
        var myConversationIds = await GetMyConversationIdsAsync(currentUserId);
        if (myConversationIds.Count == 0)
            return 0;

        var groupOf = await _conversations.GetGroupIdsAsync(myConversationIds);
        var groupChatSet = await _conversations.GetGroupChatIdsAsync(myConversationIds);
        var directIds = myConversationIds.Where(id => !groupChatSet.Contains(id)).ToList();

        var count = directIds.Count == 0 ? 0 : await _messages.GetAll(m =>
                directIds.Contains(m.ConversationId) && m.SenderId != currentUserId && !m.IsRead && m.DeletedAt == null)
            .CountAsync();

        if (groupChatSet.Count == 0)
            return count;

        var mutedGroups = await MutedGroupIdsAsync(currentUserId, groupOf.Values.Distinct().ToList());
        var mutedCustom = (await _participants.GetAll(p => p.UserId == currentUserId && p.IsMuted && groupChatSet.Contains(p.ConversationId), asNoTracking: true)
            .Select(p => p.ConversationId)
            .ToListAsync()).ToHashSet();
        var groupChatIds = groupChatSet
            .Where(id => !mutedCustom.Contains(id) && !(groupOf.TryGetValue(id, out var linkedGroup) && mutedGroups.Contains(linkedGroup)))
            .ToList();
        var hidden = await _blocks.GetBlockedIdsAsync(currentUserId);

        return count + (await _messages.CountGroupUnreadAsync(currentUserId, groupChatIds, hidden)).Values.Sum();
    }

    public async Task<bool> CanMessageAsync(Guid currentUserId, Guid otherUserId)
    {
        if (currentUserId == otherUserId || await _blocks.AreBlockedAsync(currentUserId, otherUserId))
            return false;

        return await _conversations.FindDirectAsync(currentUserId, otherUserId) != null
            || await CanStartConversationAsync(currentUserId, otherUserId);
    }


    public async Task<GetConversationDto> OpenGroupChatAsync(Guid currentUserId, Guid groupId)
    {
        if (!await _groupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == currentUserId))
            throw new NotFoundException(Loc.T("group.notFound"));

        var conversationId = await _conversations.GetOrCreateGroupAsync(groupId);
        if (!await _participants.AnyAsync(p => p.ConversationId == conversationId && p.UserId == currentUserId))
            await _conversations.SyncGroupChatsAsync(currentUserId);

        return (await BuildConversationsAsync(currentUserId, new List<Guid> { conversationId })).Single();
    }

    public async Task AddToGroupChatAsync(Guid groupId, Guid userId)
    {
        var conversationId = await _conversations.FindGroupAsync(groupId);
        if (conversationId == null || await _participants.AnyAsync(p => p.ConversationId == conversationId && p.UserId == userId))
            return;

        await _participants.AddAsync(new ConversationParticipant { ConversationId = conversationId.Value, UserId = userId });
        try
        {
            await _participants.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
        }
    }

    public async Task RemoveFromGroupChatAsync(Guid groupId, Guid userId)
    {
        var conversationId = await _conversations.FindGroupAsync(groupId);
        if (conversationId == null)
            return;

        var participant = await _participants.GetAll(p => p.ConversationId == conversationId && p.UserId == userId).FirstOrDefaultAsync();
        if (participant == null)
            return;

        _participants.Delete(participant);
        await _participants.SaveChangesAsync();

        await _notifier.PublishToMessagesAsync(userId, "ConversationRemoved", new { conversationId = conversationId.Value });
    }

    public Task<List<string>> GetGroupChatFilesAsync(Guid groupId) => _conversations.GetGroupChatMediaUrlsAsync(groupId);

    public async Task<GetConversationDto> GetConversationAsync(Guid currentUserId, Guid conversationId)
    {
        await EnsureParticipantAsync(currentUserId, conversationId);

        var rows = await BuildConversationsAsync(currentUserId, new List<Guid> { conversationId });
        return rows.FirstOrDefault() ?? throw new NotFoundException(Loc.T("conversation.notFound"));
    }


    private async Task<bool> CanStartConversationAsync(Guid currentUserId, Guid recipientId)
    {
        var recipient = await _users.GetSummaryAsync(recipientId);
        if (recipient is { IsPrivateAccount: false })
            return true;

        return await _friends.AreFriendsAsync(currentUserId, recipientId);
    }

    private async Task<List<Guid>> GetMyConversationIdsAsync(Guid userId)
    {
        var ids = await _participants.GetAll(p => p.UserId == userId, asNoTracking: true)
            .Select(p => p.ConversationId)
            .ToListAsync();

        var blockedIds = await _blocks.GetBlockedIdsAsync(userId);
        if (ids.Count == 0 || blockedIds.Count == 0)
            return ids;

        var hidden = await _participants.GetAll(
                p => ids.Contains(p.ConversationId) && blockedIds.Contains(p.UserId) && p.Conversation!.DirectKey != null,
                asNoTracking: true)
            .Select(p => p.ConversationId)
            .ToListAsync();

        return ids.Except(hidden).ToList();
    }

    private async Task EnsureParticipantAsync(Guid userId, Guid conversationId)
    {
        if (!await _participants.AnyAsync(p => p.ConversationId == conversationId && p.UserId == userId))
            throw new NotFoundException(Loc.T("conversation.notFound"));
    }

    private async Task<bool> IsGroupChatAsync(Guid conversationId)
        => (await _conversations.GetGroupChatIdsAsync(new[] { conversationId })).Count > 0;

    private async Task<HashSet<Guid>> MutedGroupIdsAsync(Guid userId, List<Guid> groupIds)
    {
        if (groupIds.Count == 0)
            return new HashSet<Guid>();

        return (await _groupMembers.GetAll(m => m.UserId == userId && m.IsMuted && groupIds.Contains(m.GroupId), asNoTracking: true)
                .Select(m => m.GroupId)
                .ToListAsync())
            .ToHashSet();
    }

    private async Task<List<Guid>> VisibleToOthersAsync(Message message, List<ConversationParticipant> participants, Guid currentUserId)
    {
        var others = participants.Where(p => p.UserId != currentUserId).Select(p => p.UserId).ToList();
        if (!await IsGroupChatAsync(message.ConversationId))
            return others;

        var hidden = await _blocks.GetBlockedIdsAsync(message.SenderId);
        return others.Where(id => !hidden.Contains(id)).ToList();
    }

    private static DateTime ReadByAllOthers(List<ConversationParticipant> participants, Guid userId)
    {
        var others = participants.Where(p => p.UserId != userId).ToList();
        return others.Count == 0 ? DateTime.MinValue : others.Min(p => p.ReadUpTo);
    }

    private static string PreviewText(Message message)
    {
        if (message.IsDeleted)
            return Loc.T("message.preview.deleted");

        return message.Type == MessageType.Image ? Loc.T("message.preview.photo") : message.Text;
    }


    private sealed class MessageContext
    {
        public Dictionary<Guid, GetStoryReplyDto> Stories { get; init; } = new();
        public Dictionary<Guid, Message> Quoted { get; init; } = new();
        public Dictionary<Guid, List<MessageReaction>> Reactions { get; init; } = new();
        public HashSet<Guid> GroupChatIds { get; init; } = new();
        public Dictionary<Guid, List<ConversationParticipant>> GroupParticipants { get; init; } = new();
        public Dictionary<Guid, UserSummary> Users { get; init; } = new();

        public Dictionary<Guid, HashSet<Guid>> HiddenFromQuotedAuthor { get; init; } = new();
    }

    private async Task<MessageContext> LoadContextAsync(List<Message> messages)
    {
        var stories = await LoadStoryRepliesAsync(messages.Where(m => m.ReplyToStoryId.HasValue).Select(m => m.ReplyToStoryId!.Value));

        var quotedIds = messages.Where(m => m.ReplyToMessageId.HasValue).Select(m => m.ReplyToMessageId!.Value).Distinct().ToList();
        var quoted = quotedIds.Count == 0
            ? new Dictionary<Guid, Message>()
            : await _messages.GetAll(m => quotedIds.Contains(m.Id), asNoTracking: true).ToDictionaryAsync(m => m.Id);

        var reactions = (await _reactions.GetForMessagesAsync(messages.Select(m => m.Id).ToList()))
            .GroupBy(r => r.MessageId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var groupChatIds = await _conversations.GetGroupChatIdsAsync(messages.Select(m => m.ConversationId).Distinct().ToList());
        var groupParticipants = groupChatIds.Count == 0
            ? new Dictionary<Guid, List<ConversationParticipant>>()
            : (await _participants.GetAll(p => groupChatIds.Contains(p.ConversationId), asNoTracking: true).ToListAsync())
                .GroupBy(p => p.ConversationId)
                .ToDictionary(g => g.Key, g => g.ToList());

        var groupMessages = messages.Where(m => groupChatIds.Contains(m.ConversationId)).ToList();
        var hiddenFromQuotedAuthor = new Dictionary<Guid, HashSet<Guid>>();
        foreach (var authorId in groupMessages
                     .Where(m => m.ReplyToMessageId.HasValue && quoted.ContainsKey(m.ReplyToMessageId.Value))
                     .Select(m => quoted[m.ReplyToMessageId!.Value].SenderId)
                     .Distinct())
            hiddenFromQuotedAuthor[authorId] = await _blocks.GetBlockedIdsAsync(authorId);

        var users = await _users.GetSummariesAsync(quoted.Values.Select(m => m.SenderId).Concat(groupMessages.Select(m => m.SenderId)));

        return new MessageContext
        {
            Stories = stories,
            Quoted = quoted,
            Reactions = reactions,
            GroupChatIds = groupChatIds,
            GroupParticipants = groupParticipants,
            Users = users,
            HiddenFromQuotedAuthor = hiddenFromQuotedAuthor
        };
    }

    private GetMessageDto Build(Message message, Guid viewerId, MessageContext context)
    {
        var dto = _mapper.Map<GetMessageDto>(message);
        dto.IsMine = message.SenderId == viewerId;
        var isGroup = context.GroupChatIds.Contains(message.ConversationId);

        if (message.ReplyToStoryId is { } storyId)
            dto.StoryReply = context.Stories[storyId];

        if (message.ReplyToMessageId is { } quotedId)
            dto.ReplyTo = QuoteFor(quotedId, viewerId, isGroup, context);

        if (context.Reactions.TryGetValue(message.Id, out var onThis))
            dto.Reactions = SummarizeReactions(onThis, viewerId);

        if (isGroup)
        {
            if (context.Users.TryGetValue(message.SenderId, out var sender))
            {
                dto.SenderName = sender.FullName;
                dto.SenderAvatarUrl = sender.AvatarUrl;
            }

            var participants = context.GroupParticipants.GetValueOrDefault(message.ConversationId) ?? new List<ConversationParticipant>();
            dto.IsRead = dto.IsMine && participants.Any(p => p.UserId != viewerId) && message.CreatedAt <= ReadByAllOthers(participants, viewerId);
        }

        return dto;
    }

    private static GetMessageReplyDto QuoteFor(Guid quotedId, Guid viewerId, bool isGroup, MessageContext context)
    {
        if (!context.Quoted.TryGetValue(quotedId, out var message)
            || (isGroup && context.HiddenFromQuotedAuthor.TryGetValue(message.SenderId, out var hidden) && hidden.Contains(viewerId)))
            return new GetMessageReplyDto { MessageId = quotedId, IsDeleted = true };

        var text = message.IsDeleted ? null : message.Text;
        if (text != null && text.Length > MessageReactions.QuotePreviewLength)
            text = text[..MessageReactions.QuotePreviewLength] + "…";

        return new GetMessageReplyDto
        {
            MessageId = message.Id,
            SenderId = message.SenderId,
            SenderName = context.Users.TryGetValue(message.SenderId, out var author) ? author.FullName : string.Empty,
            IsMine = message.SenderId == viewerId,
            Type = message.Type.ToString(),
            Text = text,
            MediaUrl = message.IsDeleted ? null : message.MediaUrl,
            IsDeleted = message.IsDeleted
        };
    }

    private async Task<List<GetMessageDto>> MapMessagesAsync(List<Message> messages, Guid currentUserId)
    {
        var context = await LoadContextAsync(messages);
        return messages.Select(m => Build(m, currentUserId, context)).ToList();
    }

    private async Task<Dictionary<Guid, GetMessageDto>> MapForViewersAsync(Message message, List<Guid> viewerIds)
    {
        var context = await LoadContextAsync(new List<Message> { message });
        return viewerIds.Distinct().ToDictionary(id => id, id => Build(message, id, context));
    }

    private static List<GetMessageReactionDto> SummarizeReactions(List<MessageReaction> reactions, Guid currentUserId)
    {
        return reactions
            .GroupBy(r => r.Emoji)
            .Select(g => new GetMessageReactionDto { Emoji = g.Key, Count = g.Count(), ReactedByMe = g.Any(r => r.UserId == currentUserId) })
            .OrderByDescending(r => r.Count)
            .ThenBy(r => MessageReactions.Allowed.ToList().IndexOf(r.Emoji))
            .ToList();
    }

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
