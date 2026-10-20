using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.DTOs.Groups;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class GroupService : IGroupService
{
    // GetGroupDto.MemberCount is computed from the Members collection.
    private static readonly string[] MemberIncludes = { "Members" };

    private static readonly string[] GroupAndInviteIncludes = { "Group" };

    // The group's small round picture has a folder of its own (UploadAccessService: open to every signed-in person, like the covers).
    private const string AvatarFolder = "group-avatars";

    private readonly IGroupRepository _groups;
    private readonly IGroupMemberRepository _members;
    private readonly IGroupJoinRequestRepository _joinRequests;
    private readonly IGroupInviteRepository _invites;
    private readonly IGroupRuleRepository _rules;
    private readonly IUserRepository _users;
    private readonly INotificationService _notifications;
    private readonly IEventRepository _events;
    private readonly IEventService _eventService;
    private readonly IMessageService _chat;
    private readonly IFileStorageService _files;
    private readonly IPostRepository _posts;
    private readonly IMapper _mapper;
    private readonly IBlockService _blocks;

    public GroupService(
        IGroupRepository groups,
        IGroupMemberRepository members,
        IGroupJoinRequestRepository joinRequests,
        IGroupInviteRepository invites,
        IGroupRuleRepository rules,
        IUserRepository users,
        INotificationService notifications,
        IEventRepository events,
        IEventService eventService,
        IMessageService chat,
        IFileStorageService files,
        IPostRepository posts,
        IMapper mapper,
        IBlockService blocks)
    {
        _rules = rules;
        _chat = chat;
        _blocks = blocks;
        _posts = posts;
        _events = events;
        _eventService = eventService;
        _groups = groups;
        _members = members;
        _joinRequests = joinRequests;
        _invites = invites;
        _users = users;
        _notifications = notifications;
        _files = files;
        _mapper = mapper;
    }

    public async Task<GetGroupDto> CreateAsync(Guid currentUserId, PostGroupDto dto)
    {
        if (!Enum.TryParse<GroupPrivacy>(dto.Privacy, out var privacy))
            throw new BadRequestException(Loc.T("group.privacy"));

        // A picture that was stored but whose group could not be saved (or whose second picture was refused) is deleted again.
        string? coverUrl = null;
        string? avatarUrl = null;
        Group group;
        try
        {
            if (dto.CoverImage != null)
                coverUrl = await _files.SaveImageAsync(dto.CoverImage, "groups");
            if (dto.Avatar != null)
                avatarUrl = await _files.SaveImageAsync(dto.Avatar, AvatarFolder);

            group = new Group
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                CoverImageUrl = coverUrl,
                AvatarUrl = avatarUrl,
                Privacy = privacy,
                CreatedByUserId = currentUserId
            };
            group.Members.Add(new GroupMember { UserId = currentUserId, Role = GroupMemberRole.Admin });

            await _groups.AddAsync(group);
            await _groups.SaveChangesAsync();
        }
        catch
        {
            _files.Delete(coverUrl);
            _files.Delete(avatarUrl);
            throw;
        }

        return await ToDtoAsync(group, currentUserId, includeMembers: false);
    }

    public async Task<GetGroupDto> UpdateAsync(Guid currentUserId, Guid groupId, PutGroupDto dto)
    {
        var group = await _groups.GetByIdAsync(groupId, MemberIncludes)
            ?? throw new NotFoundException(Loc.T("group.notFound"));

        EnsureAdmin(group, currentUserId);

        if (!Enum.TryParse<GroupPrivacy>(dto.Privacy, out var privacy))
            throw new BadRequestException(Loc.T("group.privacy"));

        // The old cover file is deleted only once the new state is saved: if the save fails, the group must not be left pointing
        // at a deleted file. A new cover that could not be saved is deleted again.
        // The avatar follows the same rule.
        var oldCoverUrl = group.CoverImageUrl;
        var oldAvatarUrl = group.AvatarUrl;
        string? newCoverUrl = null;
        string? newAvatarUrl = null;
        if (dto.CoverImage != null)
        {
            newCoverUrl = await _files.SaveImageAsync(dto.CoverImage, "groups");
            group.CoverImageUrl = newCoverUrl;
        }
        else if (dto.RemoveCoverImage)
        {
            group.CoverImageUrl = null;
        }

        if (dto.Avatar != null)
        {
            try
            {
                newAvatarUrl = await _files.SaveImageAsync(dto.Avatar, AvatarFolder);
            }
            catch
            {
                _files.Delete(newCoverUrl);
                throw;
            }
            group.AvatarUrl = newAvatarUrl;
        }
        else if (dto.RemoveAvatar)
        {
            group.AvatarUrl = null;
        }

        group.Name = dto.Name.Trim();
        group.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

        var wasPrivate = group.Privacy == GroupPrivacy.Private;
        group.Privacy = privacy;

        try
        {
            await _groups.SaveChangesAsync();
        }
        catch
        {
            _files.Delete(newCoverUrl);
            _files.Delete(newAvatarUrl);
            throw;
        }

        if (group.CoverImageUrl != oldCoverUrl)
            _files.Delete(oldCoverUrl);
        if (group.AvatarUrl != oldAvatarUrl)
            _files.Delete(oldAvatarUrl);

        // Going public makes any pending requests moot — those users can just join directly now.
        if (wasPrivate && privacy == GroupPrivacy.Public)
        {
            var pending = await _joinRequests.GetAll(r => r.GroupId == groupId).ToListAsync();
            if (pending.Count > 0)
            {
                foreach (var request in pending)
                    _joinRequests.Delete(request);
                await _joinRequests.SaveChangesAsync();
            }
        }

        return await ToDtoAsync(group, currentUserId, includeMembers: false);
    }

    public async Task<GetGroupDto> GetByIdAsync(Guid currentUserId, Guid groupId)
    {
        var group = await _groups.GetByIdAsync(groupId, MemberIncludes)
            ?? throw new NotFoundException(Loc.T("group.notFound"));

        // Private groups behave as if they don't exist for non-members.
        if (group.Privacy == GroupPrivacy.Private && group.Members.All(m => m.UserId != currentUserId))
            throw new NotFoundException(Loc.T("group.notFound"));

        return await ToDtoAsync(group, currentUserId, includeMembers: true);
    }

    public async Task<List<GetGroupDto>> GetMyGroupsAsync(Guid currentUserId)
    {
        var groups = await _groups.GetAll(
                filter: g => g.Members.Any(m => m.UserId == currentUserId),
                orderBy: g => g.CreatedAt,
                isDescending: true,
                asNoTracking: true)
            .ToListAsync();

        return await ToListDtosAsync(groups, currentUserId);
    }

    public async Task<List<GetGroupDto>> DiscoverAsync(Guid currentUserId, string? search)
    {
        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        // Private groups are listed too — just name/member-count/privacy, same as public ones.
        // Their card offers a join *request* instead of an immediate join (see JoinAsync).
        var groups = await _groups.GetAll(
                filter: g => g.Members.All(m => m.UserId != currentUserId)
                             && (term == null || g.Name.Contains(term)),
                orderBy: g => g.CreatedAt,
                isDescending: true,
                asNoTracking: true)
            .ToListAsync();

        if (groups.Count == 0)
            return new List<GetGroupDto>();

        var dtos = await ToListDtosAsync(groups, currentUserId);

        // One batched query for "did I already request to join any of these" instead of one per group.
        var groupIds = groups.Select(g => g.Id).ToList();
        var pendingGroupIds = (await _joinRequests
                .GetAll(r => r.UserId == currentUserId && groupIds.Contains(r.GroupId), asNoTracking: true)
                .Select(r => r.GroupId)
                .ToListAsync())
            .ToHashSet();

        foreach (var dto in dtos)
            dto.HasPendingJoinRequest = pendingGroupIds.Contains(dto.Id);

        return dtos;
    }

    public async Task<GetGroupDto> JoinAsync(Guid currentUserId, Guid groupId, bool acceptRules)
    {
        var group = await _groups.GetByIdAsync(groupId, MemberIncludes)
            ?? throw new NotFoundException(Loc.T("group.notFound"));

        if (group.Members.Any(m => m.UserId == currentUserId))
            throw new ConflictException(Loc.T("group.alreadyMember"));

        // Asking to join a private group counts as joining: the rules are accepted then, not again on approval.
        await EnsureRulesAcceptedAsync(groupId, acceptRules);

        if (group.Privacy == GroupPrivacy.Private)
        {
            if (await _joinRequests.AnyAsync(r => r.GroupId == groupId && r.UserId == currentUserId))
                throw new ConflictException(Loc.T("group.alreadyRequested"));

            await _joinRequests.AddAsync(new GroupJoinRequest { GroupId = groupId, UserId = currentUserId });
            await _joinRequests.SaveChangesAsync();

            foreach (var adminId in group.Members.Where(m => m.Role == GroupMemberRole.Admin).Select(m => m.UserId))
                await _notifications.CreateAsync(adminId, currentUserId, NotificationType.GroupJoinRequestReceived, groupId: groupId);

            var pendingDto = await ToDtoAsync(group, currentUserId, includeMembers: false);
            pendingDto.HasPendingJoinRequest = true;
            return pendingDto;
        }

        var member = new GroupMember { GroupId = groupId, UserId = currentUserId, Role = GroupMemberRole.Member };
        await _members.AddAsync(member);
        await _members.SaveChangesAsync();
        await _chat.AddToGroupChatAsync(groupId, currentUserId);

        // The tracked Members collection was fixed up by EF when the new row was added.
        return await ToDtoAsync(group, currentUserId, includeMembers: false);
    }

    public async Task<List<GetJoinRequestDto>> GetJoinRequestsAsync(Guid currentUserId, Guid groupId)
    {
        var group = await _groups.GetByIdAsync(groupId, MemberIncludes)
            ?? throw new NotFoundException(Loc.T("group.notFound"));

        EnsureAdmin(group, currentUserId);

        var requests = await _joinRequests.GetAll(
                filter: r => r.GroupId == groupId,
                orderBy: r => r.CreatedAt,
                asNoTracking: true)
            .ToListAsync();

        if (requests.Count == 0)
            return new List<GetJoinRequestDto>();

        var summaries = await _users.GetSummariesAsync(requests.Select(r => r.UserId));

        // A request whose account is gone (deleted before requests were cleaned up with it) is not listed.
        return requests.Where(r => summaries.ContainsKey(r.UserId)).Select(r =>
        {
            summaries.TryGetValue(r.UserId, out var summary);
            return new GetJoinRequestDto
            {
                UserId = r.UserId,
                FullName = summary?.FullName ?? "",
                UserName = summary?.UserName ?? "",
                AvatarUrl = summary?.AvatarUrl,
                RequestedAt = r.CreatedAt
            };
        }).ToList();
    }

    public async Task ApproveJoinRequestAsync(Guid currentUserId, Guid groupId, Guid requesterUserId)
    {
        var group = await _groups.GetByIdAsync(groupId, MemberIncludes)
            ?? throw new NotFoundException(Loc.T("group.notFound"));

        EnsureAdmin(group, currentUserId);

        var request = await _joinRequests.GetAll(r => r.GroupId == groupId && r.UserId == requesterUserId).FirstOrDefaultAsync()
            ?? throw new NotFoundException(Loc.T("joinRequest.notFound"));

        // The person's account may be gone: nobody to let in, the request is simply removed.
        if (!await _users.ExistsAsync(requesterUserId))
        {
            _joinRequests.Delete(request);
            await _joinRequests.SaveChangesAsync();
            throw new NotFoundException(Loc.T("user.notFound"));
        }

        _joinRequests.Delete(request);
        await _members.AddAsync(new GroupMember { GroupId = groupId, UserId = requesterUserId, Role = GroupMemberRole.Member });
        await _joinRequests.SaveChangesAsync();
        await _chat.AddToGroupChatAsync(groupId, requesterUserId);

        await _notifications.CreateAsync(requesterUserId, currentUserId, NotificationType.GroupJoinRequestApproved, groupId: groupId);
    }

    public async Task RejectJoinRequestAsync(Guid currentUserId, Guid groupId, Guid requesterUserId)
    {
        var group = await _groups.GetByIdAsync(groupId, MemberIncludes)
            ?? throw new NotFoundException(Loc.T("group.notFound"));

        EnsureAdmin(group, currentUserId);

        var request = await _joinRequests.GetAll(r => r.GroupId == groupId && r.UserId == requesterUserId).FirstOrDefaultAsync()
            ?? throw new NotFoundException(Loc.T("joinRequest.notFound"));

        _joinRequests.Delete(request);
        await _joinRequests.SaveChangesAsync();

        await _notifications.CreateAsync(requesterUserId, currentUserId, NotificationType.GroupJoinRequestRejected, groupId: groupId);
    }

    public async Task InviteMemberAsync(Guid currentUserId, Guid groupId, Guid targetUserId)
    {
        var group = await _groups.GetByIdAsync(groupId, MemberIncludes)
            ?? throw new NotFoundException(Loc.T("group.notFound"));

        EnsureAdmin(group, currentUserId);

        if (currentUserId == targetUserId)
            throw new BadRequestException(Loc.T("invite.self"));

        if (!await _users.ExistsAsync(targetUserId))
            throw new NotFoundException(Loc.T("user.notFound"));

        // Blocked people can't send each other invitations, whichever of them blocked the other.
        if (await _blocks.AreBlockedAsync(currentUserId, targetUserId))
            throw new ForbiddenException(Loc.T("block.cannotInvite"));

        if (group.Members.Any(m => m.UserId == targetUserId))
            throw new ConflictException(Loc.T("group.memberAlready"));

        // Mutual intent already exists (they already asked to join) — just let them straight in.
        var existingRequest = await _joinRequests.GetAll(r => r.GroupId == groupId && r.UserId == targetUserId).FirstOrDefaultAsync();
        if (existingRequest != null)
        {
            _joinRequests.Delete(existingRequest);
            await _members.AddAsync(new GroupMember { GroupId = groupId, UserId = targetUserId, Role = GroupMemberRole.Member });
            await _joinRequests.SaveChangesAsync();
            await _chat.AddToGroupChatAsync(groupId, targetUserId);
            return;
        }

        if (await _invites.AnyAsync(i => i.GroupId == groupId && i.UserId == targetUserId))
            throw new ConflictException(Loc.T("group.invitedAlready"));

        await _invites.AddAsync(new GroupInvite { GroupId = groupId, UserId = targetUserId, InvitedByUserId = currentUserId });
        await _invites.SaveChangesAsync();

        await _notifications.CreateAsync(targetUserId, currentUserId, NotificationType.GroupInviteReceived, groupId: groupId);
    }

    public async Task<List<GetGroupInviteDto>> GetMyInvitesAsync(Guid currentUserId)
    {
        // Invitations from someone blocked with me, also the ones sent before the block, are not shown.
        var hidden = await _blocks.GetBlockedIdsAsync(currentUserId);

        var invites = await _invites.GetAll(
                filter: i => i.UserId == currentUserId && !hidden.Contains(i.InvitedByUserId),
                orderBy: i => i.CreatedAt,
                isDescending: true,
                asNoTracking: true,
                includes: GroupAndInviteIncludes)
            .ToListAsync();

        if (invites.Count == 0)
            return new List<GetGroupInviteDto>();

        var inviters = await _users.GetSummariesAsync(invites.Select(i => i.InvitedByUserId));
        var ruleCounts = await _rules.CountByGroupAsync(invites.Select(i => i.GroupId).Distinct().ToList());
        return invites.Select(i =>
        {
            inviters.TryGetValue(i.InvitedByUserId, out var inviter);
            return new GetGroupInviteDto
            {
                GroupId = i.GroupId,
                GroupName = i.Group?.Name ?? "",
                GroupCoverImageUrl = i.Group?.CoverImageUrl,
                GroupAvatarUrl = i.Group?.AvatarUrl,
                GroupRuleCount = ruleCounts.GetValueOrDefault(i.GroupId),
                InvitedByUserId = i.InvitedByUserId,
                InvitedByName = inviter?.FullName ?? "",
                CreatedAt = i.CreatedAt
            };
        }).ToList();
    }

    public async Task AcceptInviteAsync(Guid currentUserId, Guid groupId, bool acceptRules)
    {
        var invite = await _invites.GetAll(i => i.GroupId == groupId && i.UserId == currentUserId).FirstOrDefaultAsync()
            ?? throw new NotFoundException(Loc.T("groupInvite.notFound"));

        var group = await _groups.GetByIdAsync(groupId, MemberIncludes)
            ?? throw new NotFoundException(Loc.T("group.notFound"));

        if (group.Members.Any(m => m.UserId == currentUserId))
        {
            // Already a member somehow (e.g. joined another way meanwhile) — just clear the stale invite.
            _invites.Delete(invite);
            await _invites.SaveChangesAsync();
            return;
        }

        await EnsureRulesAcceptedAsync(groupId, acceptRules);

        _invites.Delete(invite);
        await _members.AddAsync(new GroupMember { GroupId = groupId, UserId = currentUserId, Role = GroupMemberRole.Member });
        await _invites.SaveChangesAsync();
        await _chat.AddToGroupChatAsync(groupId, currentUserId);
    }

    public async Task DeclineInviteAsync(Guid currentUserId, Guid groupId)
    {
        var invite = await _invites.GetAll(i => i.GroupId == groupId && i.UserId == currentUserId).FirstOrDefaultAsync()
            ?? throw new NotFoundException(Loc.T("groupInvite.notFound"));

        _invites.Delete(invite);
        await _invites.SaveChangesAsync();
    }

    public async Task LeaveAsync(Guid currentUserId, Guid groupId)
    {
        var group = await _groups.GetByIdAsync(groupId, MemberIncludes)
            ?? throw new NotFoundException(Loc.T("group.notFound"));

        var membership = group.Members.FirstOrDefault(m => m.UserId == currentUserId)
            ?? throw new BadRequestException(Loc.T("group.notMember"));

        var others = group.Members.Where(m => m.UserId != currentUserId).ToList();

        if (others.Count == 0)
        {
            // Last member out: nothing left to keep.
            await DeleteGroupWithEventsAsync(group);
            return;
        }

        // The owner can't slip out unnoticed: they must hand the group to someone else first.
        if (group.CreatedByUserId == currentUserId)
            throw new BadRequestException(Loc.T("group.ownerMustTransfer"));

        // Never leave a group without an admin: the last admin must promote someone else first.
        if (membership.Role == GroupMemberRole.Admin && others.All(m => m.Role != GroupMemberRole.Admin))
            throw new BadRequestException(Loc.T("group.lastAdmin"));

        _members.Delete(membership);
        await _members.SaveChangesAsync();
        await _chat.RemoveFromGroupChatAsync(groupId, currentUserId);

        await _eventService.RemoveUserFromGroupEventsAsync(currentUserId, groupId, group.CreatedByUserId);
    }

    public async Task DeleteAsync(Guid currentUserId, Guid groupId, bool asAdmin = false)
    {
        var group = await _groups.GetByIdAsync(groupId, MemberIncludes)
            ?? throw new NotFoundException(Loc.T("group.notFound"));

        var isAdmin = group.Members.Any(m => m.UserId == currentUserId && m.Role == GroupMemberRole.Admin);
        if (!asAdmin && !isAdmin)
            throw new ForbiddenException(Loc.T("group.deleteAdminOnly"));

        await DeleteGroupWithEventsAsync(group);
    }

    // Deletes the group. Its events and its wall's posts go with it (the database cascades), but their files and
    // notifications are not the database's to clean up, so collect them first and remove them afterwards.
    private async Task DeleteGroupWithEventsAsync(Group group)
    {
        var groupEvents = await _events.GetAll(e => e.GroupId == group.Id, asNoTracking: true)
            .Select(e => new { e.Id, e.CoverImageUrl })
            .ToListAsync();

        var eventPhotoFiles = await _events.GetAll(e => e.GroupId == group.Id, asNoTracking: true)
            .SelectMany(e => e.Photos)
            .Select(p => p.Url)
            .ToListAsync();

        var wallPosts = _posts.GetAll(p => p.GroupId == group.Id, asNoTracking: true);
        var wallPostIds = await wallPosts.Select(p => p.Id).ToListAsync();
        var wallFiles = await wallPosts.SelectMany(p => p.Media).Select(m => m.Url).ToListAsync();

        // The group's chat (and its messages) go with it too.
        var chatFiles = await _chat.GetGroupChatFilesAsync(group.Id);

        _groups.Delete(group);
        await _groups.SaveChangesAsync();

        _files.Delete(group.CoverImageUrl);
        _files.Delete(group.AvatarUrl);

        foreach (var file in wallFiles.Concat(eventPhotoFiles).Concat(chatFiles))
            _files.Delete(file);

        await _notifications.DeleteByPostsAsync(wallPostIds);
        await _notifications.DeleteByGroupAsync(group.Id);

        foreach (var groupEvent in groupEvents)
        {
            _files.Delete(groupEvent.CoverImageUrl);
            await _notifications.DeleteByEventAsync(groupEvent.Id);
        }
    }

    public async Task RemoveMemberAsync(Guid currentUserId, Guid groupId, Guid targetUserId)
    {
        if (currentUserId == targetUserId)
            throw new BadRequestException(Loc.T("group.removeSelf"));

        var group = await _groups.GetByIdAsync(groupId, MemberIncludes)
            ?? throw new NotFoundException(Loc.T("group.notFound"));

        var acting = group.Members.FirstOrDefault(m => m.UserId == currentUserId)
            ?? throw new ForbiddenException(Loc.T("group.notMember"));

        if (acting.Role == GroupMemberRole.Member)
            throw new ForbiddenException(Loc.T("group.removeNeedsAdminOrMod"));

        var target = group.Members.FirstOrDefault(m => m.UserId == targetUserId)
            ?? throw new NotFoundException(Loc.T("group.targetNotMember"));

        // Admins can only be removed by leaving on their own (after handing off admin/ownership).
        if (target.Role == GroupMemberRole.Admin)
            throw new ForbiddenException(Loc.T("group.removeAdminDenied"));

        // Moderators can clear out regular members but can't touch each other.
        if (acting.Role == GroupMemberRole.Moderator && target.Role == GroupMemberRole.Moderator)
            throw new ForbiddenException(Loc.T("group.modRemoveMod"));

        _members.Delete(target);
        await _members.SaveChangesAsync();
        await _chat.RemoveFromGroupChatAsync(groupId, targetUserId);

        await _eventService.RemoveUserFromGroupEventsAsync(targetUserId, groupId, group.CreatedByUserId);

        await _notifications.CreateAsync(targetUserId, currentUserId, NotificationType.GroupMemberRemoved, groupId: groupId);
    }

    public async Task<GetGroupDto> SetMemberRoleAsync(Guid currentUserId, Guid groupId, Guid targetUserId, string role)
    {
        if (!Enum.TryParse<GroupMemberRole>(role, out var newRole))
            throw new BadRequestException(Loc.T("group.role"));

        if (currentUserId == targetUserId)
            throw new BadRequestException(Loc.T("group.roleSelf"));

        var group = await _groups.GetByIdAsync(groupId, MemberIncludes)
            ?? throw new NotFoundException(Loc.T("group.notFound"));

        var acting = group.Members.FirstOrDefault(m => m.UserId == currentUserId)
            ?? throw new ForbiddenException(Loc.T("group.notMember"));
        if (acting.Role != GroupMemberRole.Admin)
            throw new ForbiddenException(Loc.T("group.roleAdminsOnly"));

        var target = group.Members.FirstOrDefault(m => m.UserId == targetUserId)
            ?? throw new NotFoundException(Loc.T("group.targetNotMember"));

        if (target.UserId == group.CreatedByUserId && newRole != GroupMemberRole.Admin)
            throw new BadRequestException(Loc.T("group.demoteOwner"));

        if (target.Role == GroupMemberRole.Admin && newRole != GroupMemberRole.Admin
            && group.Members.Count(m => m.Role == GroupMemberRole.Admin) == 1)
            throw new BadRequestException(Loc.T("group.demoteLastAdmin"));

        target.Role = newRole;
        await _groups.SaveChangesAsync();

        await _notifications.CreateAsync(targetUserId, currentUserId, NotificationType.GroupRoleChanged, groupId: groupId);

        return await ToDtoAsync(group, currentUserId, includeMembers: true);
    }

    public async Task<GetGroupDto> TransferOwnershipAsync(Guid currentUserId, Guid groupId, Guid newOwnerUserId)
    {
        var group = await _groups.GetByIdAsync(groupId, MemberIncludes)
            ?? throw new NotFoundException(Loc.T("group.notFound"));

        if (group.CreatedByUserId != currentUserId)
            throw new ForbiddenException(Loc.T("group.transferOwnerOnly"));

        if (newOwnerUserId == currentUserId)
            throw new BadRequestException(Loc.T("group.alreadyOwner"));

        var newOwner = group.Members.FirstOrDefault(m => m.UserId == newOwnerUserId)
            ?? throw new BadRequestException(Loc.T("group.transferMember"));

        newOwner.Role = GroupMemberRole.Admin;
        group.CreatedByUserId = newOwnerUserId;
        await _groups.SaveChangesAsync();

        return await ToDtoAsync(group, currentUserId, includeMembers: true);
    }

    public async Task<List<GetGroupRuleDto>> GetRulesAsync(Guid groupId)
    {
        if (!await _groups.AnyAsync(g => g.Id == groupId))
            throw new NotFoundException(Loc.T("group.notFound"));

        return _mapper.Map<List<GetGroupRuleDto>>(await _rules.GetForGroupAsync(groupId));
    }

    public async Task<List<GetGroupRuleDto>> SetRulesAsync(Guid currentUserId, Guid groupId, PutGroupRulesDto dto)
    {
        var group = await _groups.GetByIdAsync(groupId, MemberIncludes)
            ?? throw new NotFoundException(Loc.T("group.notFound"));

        EnsureAdmin(group, currentUserId);

        // The validator says the same; this is the rule itself, for any caller.
        if (dto.Rules.Count > GroupLimits.MaxRules)
            throw new BadRequestException(Loc.T("val.group.rulesMax", GroupLimits.MaxRules));

        // The new list replaces the old one as a whole (the modal edits, adds, removes and reorders in one go).
        foreach (var old in await _rules.GetAll(r => r.GroupId == groupId).ToListAsync())
            _rules.Delete(old);

        var order = 0;
        foreach (var rule in dto.Rules)
        {
            await _rules.AddAsync(new GroupRule
            {
                GroupId = groupId,
                Title = rule.Title.Trim(),
                Text = string.IsNullOrWhiteSpace(rule.Text) ? null : rule.Text.Trim(),
                SortOrder = order++
            });
        }

        await _rules.SaveChangesAsync();
        return _mapper.Map<List<GetGroupRuleDto>>(await _rules.GetForGroupAsync(groupId));
    }

    public async Task<PagedResult<GetGroupMemberDto>> GetMembersAsync(
        Guid currentUserId, Guid groupId, string? search, string? role, int page, int pageSize)
    {
        var group = await _groups.GetByIdAsync(groupId)
            ?? throw new NotFoundException(Loc.T("group.notFound"));

        // Like the group itself: a private group's members are for its members only.
        if (group.Privacy == GroupPrivacy.Private && !await _members.AnyAsync(m => m.GroupId == groupId && m.UserId == currentUserId))
            throw new NotFoundException(Loc.T("group.notFound"));

        var ownerOnly = role == "Owner";
        GroupMemberRole? roleFilter = null;
        if (!ownerOnly && !string.IsNullOrEmpty(role))
        {
            if (!Enum.TryParse<GroupMemberRole>(role, out var parsed))
                throw new BadRequestException(Loc.T("group.memberFilter"));
            roleFilter = parsed;
        }

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, GroupLimits.MaxMembersPageSize);

        var (rows, total) = await _members.SearchAsync(groupId, group.CreatedByUserId, search, roleFilter, ownerOnly, page, pageSize);
        var summaries = await _users.GetSummariesAsync(rows.Select(m => m.UserId));

        return new PagedResult<GetGroupMemberDto>
        {
            Items = rows.Where(m => summaries.ContainsKey(m.UserId)).Select(m => new GetGroupMemberDto
            {
                UserId = m.UserId,
                FullName = summaries[m.UserId].FullName,
                UserName = summaries[m.UserId].UserName,
                AvatarUrl = summaries[m.UserId].AvatarUrl,
                Role = m.Role.ToString(),
                IsOwner = m.UserId == group.CreatedByUserId
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<GetGroupDto> SetMutedAsync(Guid currentUserId, Guid groupId, bool muted)
    {
        var group = await _groups.GetByIdAsync(groupId, MemberIncludes)
            ?? throw new NotFoundException(Loc.T("group.notFound"));

        var membership = group.Members.FirstOrDefault(m => m.UserId == currentUserId)
            ?? throw new BadRequestException(Loc.T("group.notMember"));

        if (membership.IsMuted != muted)
        {
            membership.IsMuted = muted;
            await _members.SaveChangesAsync();
        }

        return await ToDtoAsync(group, currentUserId, includeMembers: true);
    }

    // A group with rules lets nobody in who did not accept them (the page shows them with a box to tick).
    private async Task EnsureRulesAcceptedAsync(Guid groupId, bool acceptRules)
    {
        if (!acceptRules && await _rules.AnyAsync(r => r.GroupId == groupId))
            throw new BadRequestException(Loc.T("group.rulesNotAccepted"));
    }

    private static void EnsureAdmin(Group group, Guid currentUserId)
    {
        var isAdmin = group.Members.Any(m => m.UserId == currentUserId && m.Role == GroupMemberRole.Admin);
        if (!isAdmin)
            throw new ForbiddenException(Loc.T("group.adminOnly"));
    }

    // The cards of a list of groups: the member rows are not loaded, the database counts them and says what role the viewer has
    // (one query for all the groups; loading every member of every group just to count them grew with the site).
    private async Task<List<GetGroupDto>> ToListDtosAsync(IReadOnlyList<Group> groups, Guid currentUserId)
    {
        var groupIds = groups.Select(g => g.Id).ToList();
        var memberships = await _groups.GetMembershipsAsync(groupIds, currentUserId);
        var ruleCounts = await _rules.CountByGroupAsync(groupIds);

        return groups.Select(group =>
        {
            var membership = memberships[group.Id];
            var dto = _mapper.Map<GetGroupDto>(group);
            dto.MemberCount = membership.MemberCount;
            dto.RuleCount = ruleCounts.GetValueOrDefault(group.Id);
            dto.IsCurrentUserMember = membership.ViewerRole != null;
            dto.IsCurrentUserAdmin = membership.ViewerRole == GroupMemberRole.Admin;
            dto.IsCurrentUserModerator = membership.ViewerRole == GroupMemberRole.Moderator;
            dto.IsCurrentUserOwner = group.CreatedByUserId == currentUserId;
            return dto;
        }).ToList();
    }

    private async Task<GetGroupDto> ToDtoAsync(Group group, Guid currentUserId, bool includeMembers)
    {
        var dto = _mapper.Map<GetGroupDto>(group);
        dto.IsCurrentUserMember = group.Members.Any(m => m.UserId == currentUserId);
        dto.IsCurrentUserAdmin = group.Members.Any(m => m.UserId == currentUserId && m.Role == GroupMemberRole.Admin);
        dto.IsCurrentUserModerator = group.Members.Any(m => m.UserId == currentUserId && m.Role == GroupMemberRole.Moderator);
        dto.IsCurrentUserOwner = group.CreatedByUserId == currentUserId;
        dto.IsMutedByCurrentUser = group.Members.Any(m => m.UserId == currentUserId && m.IsMuted);
        dto.RuleCount = (await _rules.CountByGroupAsync(new[] { group.Id })).GetValueOrDefault(group.Id);

        if (includeMembers && group.Members.Count > 0)
        {
            var summaries = await _users.GetSummariesAsync(group.Members.Select(m => m.UserId));
            dto.Members = group.Members
                // Admin, then Moderator, then Member. (Not by the enum's number: Moderator was added after Admin, so it is the larger one.)
                .OrderBy(m => m.Role == GroupMemberRole.Admin ? 0 : m.Role == GroupMemberRole.Moderator ? 1 : 2)
                .ThenBy(m => summaries.TryGetValue(m.UserId, out var s) ? s.FullName : "")
                .Select(m =>
                {
                    summaries.TryGetValue(m.UserId, out var summary);
                    return new GetGroupMemberDto
                    {
                        UserId = m.UserId,
                        FullName = summary?.FullName ?? "",
                        UserName = summary?.UserName ?? "",
                        AvatarUrl = summary?.AvatarUrl,
                        Role = m.Role.ToString(),
                        IsOwner = m.UserId == group.CreatedByUserId
                    };
                })
                .ToList();
        }

        return dto;
    }
}
