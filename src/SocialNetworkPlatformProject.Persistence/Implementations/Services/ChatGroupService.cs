using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Messages;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class ChatGroupService : IChatGroupService
{
    private const string AvatarFolder = "chat-avatars";

    private readonly IConversationRepository _conversations;
    private readonly IConversationParticipantRepository _participants;
    private readonly IMessageService _messages;
    private readonly IFriendService _friends;
    private readonly IBlockService _blocks;
    private readonly IUserRepository _users;
    private readonly IFileStorageService _files;
    private readonly INotificationService _notifications;
    private readonly IRealTimeNotifier _notifier;

    public ChatGroupService(
        IConversationRepository conversations,
        IConversationParticipantRepository participants,
        IMessageService messages,
        IFriendService friends,
        IBlockService blocks,
        IUserRepository users,
        IFileStorageService files,
        INotificationService notifications,
        IRealTimeNotifier notifier)
    {
        _conversations = conversations;
        _participants = participants;
        _messages = messages;
        _friends = friends;
        _blocks = blocks;
        _users = users;
        _files = files;
        _notifications = notifications;
        _notifier = notifier;
    }

    public async Task<GetConversationDto> CreateAsync(Guid currentUserId, PostChatGroupDto dto)
    {
        var memberIds = dto.MemberIds.Distinct().Where(id => id != currentUserId).ToList();
        if (memberIds.Count < ChatGroupLimits.MinOtherMembers)
            throw new BadRequestException(Loc.T("val.chatGroup.members", ChatGroupLimits.MinOtherMembers));

        if (memberIds.Count + 1 > ChatGroupLimits.MaxMembers)
            throw new BadRequestException(Loc.T("chatGroup.tooMany", ChatGroupLimits.MaxMembers));

        await EnsureMayAddAsync(currentUserId, memberIds);

        var avatarUrl = dto.Avatar == null ? null : await _files.SaveImageAsync(dto.Avatar, AvatarFolder);

        var chat = new Conversation { Title = dto.Title.Trim(), AvatarUrl = avatarUrl, CreatedByUserId = currentUserId };
        chat.Participants.Add(new ConversationParticipant { UserId = currentUserId, IsAdmin = true });
        foreach (var memberId in memberIds)
            chat.Participants.Add(new ConversationParticipant { UserId = memberId });

        await _conversations.AddAsync(chat);
        try
        {
            await _conversations.SaveChangesAsync();
        }
        catch
        {
            _files.Delete(avatarUrl);
            throw;
        }

        await TellAddedAsync(currentUserId, chat.Id, memberIds);
        return await _messages.GetConversationAsync(currentUserId, chat.Id);
    }

    public async Task<GetChatGroupDto> GetAsync(Guid currentUserId, Guid conversationId)
    {
        var (chat, members, _) = await LoadAsync(currentUserId, conversationId);
        return await BuildDetailsAsync(chat, members, currentUserId);
    }

    public async Task<GetChatGroupDto> UpdateAsync(Guid currentUserId, Guid conversationId, PutChatGroupDto dto)
    {
        var (chat, members, me) = await LoadAsync(currentUserId, conversationId);
        EnsureAdmin(me);

        string? oldAvatar = null;
        string? newAvatar = null;

        if (dto.Title != null)
            chat.Title = dto.Title.Trim();

        if (dto.Avatar != null)
        {
            oldAvatar = chat.AvatarUrl;
            newAvatar = await _files.SaveImageAsync(dto.Avatar, AvatarFolder);
            chat.AvatarUrl = newAvatar;
        }
        else if (dto.RemoveAvatar && chat.AvatarUrl != null)
        {
            oldAvatar = chat.AvatarUrl;
            chat.AvatarUrl = null;
        }

        try
        {
            await _conversations.SaveChangesAsync();
        }
        catch
        {
            _files.Delete(newAvatar);
            throw;
        }

        _files.Delete(oldAvatar);

        await PublishUpdatedAsync(conversationId, members.Select(m => m.UserId));
        return await BuildDetailsAsync(chat, members, currentUserId);
    }

    public async Task<GetChatGroupDto> AddMembersAsync(Guid currentUserId, Guid conversationId, PostChatGroupMembersDto dto)
    {
        var (chat, members, me) = await LoadAsync(currentUserId, conversationId);
        EnsureAdmin(me);

        var newIds = dto.UserIds.Distinct().Where(id => members.All(m => m.UserId != id)).ToList();
        if (newIds.Count == 0)
            return await BuildDetailsAsync(chat, members, currentUserId);

        if (members.Count + newIds.Count > ChatGroupLimits.MaxMembers)
            throw new BadRequestException(Loc.T("chatGroup.tooMany", ChatGroupLimits.MaxMembers));

        await EnsureMayAddAsync(currentUserId, newIds);

        foreach (var id in newIds)
            await _participants.AddAsync(new ConversationParticipant { ConversationId = conversationId, UserId = id });

        await _participants.SaveChangesAsync();

        await TellAddedAsync(currentUserId, conversationId, newIds);
        await PublishUpdatedAsync(conversationId, members.Select(m => m.UserId));

        members.AddRange(await _participants.GetAll(p => p.ConversationId == conversationId && newIds.Contains(p.UserId), asNoTracking: true).ToListAsync());
        return await BuildDetailsAsync(chat, members, currentUserId);
    }

    public async Task<GetChatGroupDto> RemoveMemberAsync(Guid currentUserId, Guid conversationId, Guid memberId)
    {
        var (chat, members, me) = await LoadAsync(currentUserId, conversationId);
        EnsureAdmin(me);

        if (memberId == currentUserId)
            throw new BadRequestException(Loc.T("chatGroup.useLeave"));

        var target = members.FirstOrDefault(m => m.UserId == memberId)
            ?? throw new NotFoundException(Loc.T("chatGroup.memberNotFound"));

        if (target.IsAdmin)
            throw new ForbiddenException(Loc.T("chatGroup.removeAdmin"));

        _participants.Delete(target);
        await _participants.SaveChangesAsync();
        members.Remove(target);

        await _notifier.PublishToMessagesAsync(memberId, "ConversationRemoved", new { conversationId });
        await PublishUpdatedAsync(conversationId, members.Select(m => m.UserId));

        return await BuildDetailsAsync(chat, members, currentUserId);
    }

    public async Task<GetChatGroupDto> SetAdminAsync(Guid currentUserId, Guid conversationId, Guid memberId, bool isAdmin)
    {
        var (chat, members, me) = await LoadAsync(currentUserId, conversationId);
        EnsureAdmin(me);

        var target = members.FirstOrDefault(m => m.UserId == memberId)
            ?? throw new NotFoundException(Loc.T("chatGroup.memberNotFound"));

        if (target.IsAdmin != isAdmin)
        {
            if (!isAdmin && members.Count(m => m.IsAdmin) == 1)
                throw new BadRequestException(Loc.T("chatGroup.lastAdmin"));

            target.IsAdmin = isAdmin;
            await _participants.SaveChangesAsync();
            await PublishUpdatedAsync(conversationId, members.Select(m => m.UserId));
        }

        return await BuildDetailsAsync(chat, members, currentUserId);
    }

    public async Task LeaveAsync(Guid currentUserId, Guid conversationId)
    {
        var (chat, members, me) = await LoadAsync(currentUserId, conversationId);

        var others = members.Where(m => m.UserId != currentUserId).ToList();
        if (others.Count == 0)
        {
            var files = await _conversations.GetMediaUrlsAsync(conversationId);
            var avatar = chat.AvatarUrl;
            _conversations.Delete(chat);
            await _conversations.SaveChangesAsync();

            foreach (var file in files)
                _files.Delete(file);
            _files.Delete(avatar);
        }
        else
        {
            _participants.Delete(me);

            if (me.IsAdmin && others.All(m => !m.IsAdmin))
                others.OrderBy(m => m.CreatedAt).First().IsAdmin = true;

            await _participants.SaveChangesAsync();
            await PublishUpdatedAsync(conversationId, others.Select(m => m.UserId));
        }

        await _notifier.PublishToMessagesAsync(currentUserId, "ConversationRemoved", new { conversationId });
    }

    public async Task<GetChatGroupDto> SetMutedAsync(Guid currentUserId, Guid conversationId, bool muted)
    {
        var (chat, members, me) = await LoadAsync(currentUserId, conversationId);

        if (me.IsMuted != muted)
        {
            me.IsMuted = muted;
            await _participants.SaveChangesAsync();

            await _notifier.PublishToMessagesAsync(currentUserId, "ConversationUpdated", await _messages.GetConversationAsync(currentUserId, conversationId));
        }

        return await BuildDetailsAsync(chat, members, currentUserId);
    }


    private async Task<(Conversation Chat, List<ConversationParticipant> Members, ConversationParticipant Me)> LoadAsync(Guid currentUserId, Guid conversationId)
    {
        var chat = await _conversations.GetByIdAsync(conversationId);
        if (chat == null || !chat.IsCustomGroup)
            throw new NotFoundException(Loc.T("conversation.notFound"));

        var members = await _participants.GetAll(p => p.ConversationId == conversationId).ToListAsync();
        var me = members.FirstOrDefault(p => p.UserId == currentUserId)
            ?? throw new NotFoundException(Loc.T("conversation.notFound"));

        return (chat, members, me);
    }

    private static void EnsureAdmin(ConversationParticipant me)
    {
        if (!me.IsAdmin)
            throw new ForbiddenException(Loc.T("chatGroup.notAdmin"));
    }

    private async Task EnsureMayAddAsync(Guid adderId, List<Guid> userIds)
    {
        foreach (var id in userIds)
        {
            if (!await _users.ExistsAsync(id))
                throw new NotFoundException(Loc.T("user.notFound"));

            if (await _blocks.AreBlockedAsync(adderId, id))
                throw new ForbiddenException(Loc.T("chatGroup.cannot"));

            if (!await _friends.AreFriendsAsync(adderId, id))
                throw new ForbiddenException(Loc.T("chatGroup.friendsOnly"));
        }
    }

    private async Task TellAddedAsync(Guid adderId, Guid conversationId, List<Guid> addedIds)
    {
        foreach (var id in addedIds)
        {
            await _notifications.CreateAsync(id, adderId, NotificationType.GroupChatAdded, conversationId: conversationId);
            await _notifier.PublishToMessagesAsync(id, "ConversationAdded", await _messages.GetConversationAsync(id, conversationId));
        }
    }

    private async Task PublishUpdatedAsync(Guid conversationId, IEnumerable<Guid> memberIds)
    {
        foreach (var id in memberIds.Distinct().ToList())
            await _notifier.PublishToMessagesAsync(id, "ConversationUpdated", await _messages.GetConversationAsync(id, conversationId));
    }

    private async Task<GetChatGroupDto> BuildDetailsAsync(Conversation chat, List<ConversationParticipant> members, Guid viewerId)
    {
        var people = await _users.GetSummariesAsync(members.Select(m => m.UserId));
        var me = members.First(m => m.UserId == viewerId);

        return new GetChatGroupDto
        {
            Id = chat.Id,
            Title = chat.Title!,
            AvatarUrl = chat.AvatarUrl,
            MemberCount = members.Count,
            CanManage = me.IsAdmin,
            IsMuted = me.IsMuted,
            MaxMembers = ChatGroupLimits.MaxMembers,
            Members = members
                .Where(m => people.ContainsKey(m.UserId))
                .Select(m => new GetChatGroupMemberDto
                {
                    UserId = m.UserId,
                    FullName = people[m.UserId].FullName,
                    AvatarUrl = people[m.UserId].AvatarUrl,
                    IsAdmin = m.IsAdmin,
                    IsMe = m.UserId == viewerId
                })
                .OrderByDescending(m => m.IsAdmin)
                .ThenBy(m => m.FullName)
                .ToList()
        };
    }
}
