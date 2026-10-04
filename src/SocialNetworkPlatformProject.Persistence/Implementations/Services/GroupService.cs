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

    private readonly IGroupRepository _groups;
    private readonly IGroupMemberRepository _members;
    private readonly IGroupJoinRequestRepository _joinRequests;
    private readonly IGroupInviteRepository _invites;
    private readonly IUserRepository _users;
    private readonly INotificationService _notifications;
    private readonly IEventRepository _events;
    private readonly IEventService _eventService;
    private readonly IFileStorageService _files;
    private readonly IMapper _mapper;

    public GroupService(
        IGroupRepository groups,
        IGroupMemberRepository members,
        IGroupJoinRequestRepository joinRequests,
        IGroupInviteRepository invites,
        IUserRepository users,
        INotificationService notifications,
        IEventRepository events,
        IEventService eventService,
        IFileStorageService files,
        IMapper mapper)
    {
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
            throw new BadRequestException("Privacy must be either 'Public' or 'Private'.");

        string? coverUrl = null;
        if (dto.CoverImage != null)
            coverUrl = await _files.SaveImageAsync(dto.CoverImage, "groups");

        var group = new Group
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            CoverImageUrl = coverUrl,
            Privacy = privacy,
            CreatedByUserId = currentUserId
        };
        group.Members.Add(new GroupMember { UserId = currentUserId, Role = GroupMemberRole.Admin });

        await _groups.AddAsync(group);
        await _groups.SaveChangesAsync();

        return await ToDtoAsync(group, currentUserId, includeMembers: false);
    }

    public async Task<GetGroupDto> UpdateAsync(Guid currentUserId, Guid groupId, PutGroupDto dto)
    {
        var group = await _groups.GetByIdAsync(groupId, MemberIncludes)
            ?? throw new NotFoundException("Group not found.");

        EnsureAdmin(group, currentUserId);

        if (!Enum.TryParse<GroupPrivacy>(dto.Privacy, out var privacy))
            throw new BadRequestException("Privacy must be either 'Public' or 'Private'.");

        if (dto.CoverImage != null)
        {
            _files.Delete(group.CoverImageUrl);
            group.CoverImageUrl = await _files.SaveImageAsync(dto.CoverImage, "groups");
        }
        else if (dto.RemoveCoverImage)
        {
            _files.Delete(group.CoverImageUrl);
            group.CoverImageUrl = null;
        }

        group.Name = dto.Name.Trim();
        group.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

        var wasPrivate = group.Privacy == GroupPrivacy.Private;
        group.Privacy = privacy;
        await _groups.SaveChangesAsync();

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
            ?? throw new NotFoundException("Group not found.");

        // Private groups behave as if they don't exist for non-members.
        if (group.Privacy == GroupPrivacy.Private && group.Members.All(m => m.UserId != currentUserId))
            throw new NotFoundException("Group not found.");

        return await ToDtoAsync(group, currentUserId, includeMembers: true);
    }

    public async Task<List<GetGroupDto>> GetMyGroupsAsync(Guid currentUserId)
    {
        var groups = await _groups.GetAll(
                filter: g => g.Members.Any(m => m.UserId == currentUserId),
                orderBy: g => g.CreatedAt,
                isDescending: true,
                asNoTracking: true,
                includes: MemberIncludes)
            .ToListAsync();

        return (await Task.WhenAll(groups.Select(g => ToDtoAsync(g, currentUserId, includeMembers: false)))).ToList();
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
                asNoTracking: true,
                includes: MemberIncludes)
            .ToListAsync();

        if (groups.Count == 0)
            return new List<GetGroupDto>();

        // ToDtoAsync's own awaits only run when includeMembers is true, so this Task.WhenAll stays safe:
        // every task here resolves synchronously, never touching the shared DbContext concurrently.
        var dtos = (await Task.WhenAll(groups.Select(g => ToDtoAsync(g, currentUserId, includeMembers: false)))).ToList();

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

    public async Task<GetGroupDto> JoinAsync(Guid currentUserId, Guid groupId)
    {
        var group = await _groups.GetByIdAsync(groupId, MemberIncludes)
            ?? throw new NotFoundException("Group not found.");

        if (group.Members.Any(m => m.UserId == currentUserId))
            throw new ConflictException("You are already a member of this group.");

        if (group.Privacy == GroupPrivacy.Private)
        {
            if (await _joinRequests.AnyAsync(r => r.GroupId == groupId && r.UserId == currentUserId))
                throw new ConflictException("You already requested to join this group.");

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

        // The tracked Members collection was fixed up by EF when the new row was added.
        return await ToDtoAsync(group, currentUserId, includeMembers: false);
    }

    public async Task<List<GetJoinRequestDto>> GetJoinRequestsAsync(Guid currentUserId, Guid groupId)
    {
        var group = await _groups.GetByIdAsync(groupId, MemberIncludes)
            ?? throw new NotFoundException("Group not found.");

        EnsureAdmin(group, currentUserId);

        var requests = await _joinRequests.GetAll(
                filter: r => r.GroupId == groupId,
                orderBy: r => r.CreatedAt,
                asNoTracking: true)
            .ToListAsync();

        if (requests.Count == 0)
            return new List<GetJoinRequestDto>();

        var summaries = await _users.GetSummariesAsync(requests.Select(r => r.UserId));
        return requests.Select(r =>
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
            ?? throw new NotFoundException("Group not found.");

        EnsureAdmin(group, currentUserId);

        var request = await _joinRequests.GetAll(r => r.GroupId == groupId && r.UserId == requesterUserId).FirstOrDefaultAsync()
            ?? throw new NotFoundException("Join request not found.");

        _joinRequests.Delete(request);
        await _members.AddAsync(new GroupMember { GroupId = groupId, UserId = requesterUserId, Role = GroupMemberRole.Member });
        await _joinRequests.SaveChangesAsync();

        await _notifications.CreateAsync(requesterUserId, currentUserId, NotificationType.GroupJoinRequestApproved, groupId: groupId);
    }

    public async Task RejectJoinRequestAsync(Guid currentUserId, Guid groupId, Guid requesterUserId)
    {
        var group = await _groups.GetByIdAsync(groupId, MemberIncludes)
            ?? throw new NotFoundException("Group not found.");

        EnsureAdmin(group, currentUserId);

        var request = await _joinRequests.GetAll(r => r.GroupId == groupId && r.UserId == requesterUserId).FirstOrDefaultAsync()
            ?? throw new NotFoundException("Join request not found.");

        _joinRequests.Delete(request);
        await _joinRequests.SaveChangesAsync();

        await _notifications.CreateAsync(requesterUserId, currentUserId, NotificationType.GroupJoinRequestRejected, groupId: groupId);
    }

    public async Task InviteMemberAsync(Guid currentUserId, Guid groupId, Guid targetUserId)
    {
        var group = await _groups.GetByIdAsync(groupId, MemberIncludes)
            ?? throw new NotFoundException("Group not found.");

        EnsureAdmin(group, currentUserId);

        if (currentUserId == targetUserId)
            throw new BadRequestException("Kendini davet edemezsin.");

        if (!await _users.ExistsAsync(targetUserId))
            throw new NotFoundException("User not found.");

        if (group.Members.Any(m => m.UserId == targetUserId))
            throw new ConflictException("This user is already a member.");

        // Mutual intent already exists (they already asked to join) — just let them straight in.
        var existingRequest = await _joinRequests.GetAll(r => r.GroupId == groupId && r.UserId == targetUserId).FirstOrDefaultAsync();
        if (existingRequest != null)
        {
            _joinRequests.Delete(existingRequest);
            await _members.AddAsync(new GroupMember { GroupId = groupId, UserId = targetUserId, Role = GroupMemberRole.Member });
            await _joinRequests.SaveChangesAsync();
            return;
        }

        if (await _invites.AnyAsync(i => i.GroupId == groupId && i.UserId == targetUserId))
            throw new ConflictException("This user has already been invited.");

        await _invites.AddAsync(new GroupInvite { GroupId = groupId, UserId = targetUserId, InvitedByUserId = currentUserId });
        await _invites.SaveChangesAsync();

        await _notifications.CreateAsync(targetUserId, currentUserId, NotificationType.GroupInviteReceived, groupId: groupId);
    }

    public async Task<List<GetGroupInviteDto>> GetMyInvitesAsync(Guid currentUserId)
    {
        var invites = await _invites.GetAll(
                filter: i => i.UserId == currentUserId,
                orderBy: i => i.CreatedAt,
                isDescending: true,
                asNoTracking: true,
                includes: GroupAndInviteIncludes)
            .ToListAsync();

        if (invites.Count == 0)
            return new List<GetGroupInviteDto>();

        var inviters = await _users.GetSummariesAsync(invites.Select(i => i.InvitedByUserId));
        return invites.Select(i =>
        {
            inviters.TryGetValue(i.InvitedByUserId, out var inviter);
            return new GetGroupInviteDto
            {
                GroupId = i.GroupId,
                GroupName = i.Group?.Name ?? "",
                GroupCoverImageUrl = i.Group?.CoverImageUrl,
                InvitedByUserId = i.InvitedByUserId,
                InvitedByName = inviter?.FullName ?? "",
                CreatedAt = i.CreatedAt
            };
        }).ToList();
    }

    public async Task AcceptInviteAsync(Guid currentUserId, Guid groupId)
    {
        var invite = await _invites.GetAll(i => i.GroupId == groupId && i.UserId == currentUserId).FirstOrDefaultAsync()
            ?? throw new NotFoundException("Invite not found.");

        var group = await _groups.GetByIdAsync(groupId, MemberIncludes)
            ?? throw new NotFoundException("Group not found.");

        if (group.Members.Any(m => m.UserId == currentUserId))
        {
            // Already a member somehow (e.g. joined another way meanwhile) — just clear the stale invite.
            _invites.Delete(invite);
            await _invites.SaveChangesAsync();
            return;
        }

        _invites.Delete(invite);
        await _members.AddAsync(new GroupMember { GroupId = groupId, UserId = currentUserId, Role = GroupMemberRole.Member });
        await _invites.SaveChangesAsync();
    }

    public async Task DeclineInviteAsync(Guid currentUserId, Guid groupId)
    {
        var invite = await _invites.GetAll(i => i.GroupId == groupId && i.UserId == currentUserId).FirstOrDefaultAsync()
            ?? throw new NotFoundException("Invite not found.");

        _invites.Delete(invite);
        await _invites.SaveChangesAsync();
    }

    public async Task LeaveAsync(Guid currentUserId, Guid groupId)
    {
        var group = await _groups.GetByIdAsync(groupId, MemberIncludes)
            ?? throw new NotFoundException("Group not found.");

        var membership = group.Members.FirstOrDefault(m => m.UserId == currentUserId)
            ?? throw new BadRequestException("You are not a member of this group.");

        var others = group.Members.Where(m => m.UserId != currentUserId).ToList();

        if (others.Count == 0)
        {
            // Last member out: nothing left to keep.
            await DeleteGroupWithEventsAsync(group);
            return;
        }

        // The owner can't slip out unnoticed: they must hand the group to someone else first.
        if (group.CreatedByUserId == currentUserId)
            throw new BadRequestException("Grubun sahibisin. Ayrılmadan önce grubu başka bir üyeye devretmelisin.");

        // Never leave a group without an admin: the last admin must promote someone else first.
        if (membership.Role == GroupMemberRole.Admin && others.All(m => m.Role != GroupMemberRole.Admin))
            throw new BadRequestException("Grupta başka yönetici kalmıyor. Ayrılmadan önce bir üyeyi yönetici yap.");

        _members.Delete(membership);
        await _members.SaveChangesAsync();

        await _eventService.RemoveUserFromGroupEventsAsync(currentUserId, groupId, group.CreatedByUserId);
    }

    public async Task DeleteAsync(Guid currentUserId, Guid groupId)
    {
        var group = await _groups.GetByIdAsync(groupId, MemberIncludes)
            ?? throw new NotFoundException("Group not found.");

        var isAdmin = group.Members.Any(m => m.UserId == currentUserId && m.Role == GroupMemberRole.Admin);
        if (!isAdmin)
            throw new ForbiddenException("Only a group admin can delete the group.");

        await DeleteGroupWithEventsAsync(group);
    }

    // Deletes the group. Its events go with it (the database cascades), but their cover files and notifications are
    // not the database's to clean up, so collect them first and remove them afterwards.
    private async Task DeleteGroupWithEventsAsync(Group group)
    {
        var groupEvents = await _events.GetAll(e => e.GroupId == group.Id, asNoTracking: true)
            .Select(e => new { e.Id, e.CoverImageUrl })
            .ToListAsync();

        _groups.Delete(group);
        await _groups.SaveChangesAsync();

        _files.Delete(group.CoverImageUrl);

        foreach (var groupEvent in groupEvents)
        {
            _files.Delete(groupEvent.CoverImageUrl);
            await _notifications.DeleteByEventAsync(groupEvent.Id);
        }
    }

    public async Task RemoveMemberAsync(Guid currentUserId, Guid groupId, Guid targetUserId)
    {
        if (currentUserId == targetUserId)
            throw new BadRequestException("Kendini gruptan çıkaramazsın, bunun için 'Ayrıl' seçeneğini kullan.");

        var group = await _groups.GetByIdAsync(groupId, MemberIncludes)
            ?? throw new NotFoundException("Group not found.");

        var acting = group.Members.FirstOrDefault(m => m.UserId == currentUserId)
            ?? throw new ForbiddenException("Bu grubun üyesi değilsin.");

        if (acting.Role == GroupMemberRole.Member)
            throw new ForbiddenException("Üye çıkarmak için yönetici ya da moderatör olmalısın.");

        var target = group.Members.FirstOrDefault(m => m.UserId == targetUserId)
            ?? throw new NotFoundException("Bu kullanıcı grubun üyesi değil.");

        // Admins can only be removed by leaving on their own (after handing off admin/ownership).
        if (target.Role == GroupMemberRole.Admin)
            throw new ForbiddenException("Bir yöneticiyi gruptan çıkaramazsın.");

        // Moderators can clear out regular members but can't touch each other.
        if (acting.Role == GroupMemberRole.Moderator && target.Role == GroupMemberRole.Moderator)
            throw new ForbiddenException("Bir moderatör başka bir moderatörü çıkaramaz.");

        _members.Delete(target);
        await _members.SaveChangesAsync();

        await _eventService.RemoveUserFromGroupEventsAsync(targetUserId, groupId, group.CreatedByUserId);

        await _notifications.CreateAsync(targetUserId, currentUserId, NotificationType.GroupMemberRemoved, groupId: groupId);
    }

    public async Task<GetGroupDto> SetMemberRoleAsync(Guid currentUserId, Guid groupId, Guid targetUserId, string role)
    {
        if (!Enum.TryParse<GroupMemberRole>(role, out var newRole))
            throw new BadRequestException("Role must be 'Admin', 'Moderator' or 'Member'.");

        if (currentUserId == targetUserId)
            throw new BadRequestException("Kendi rolünü değiştiremezsin.");

        var group = await _groups.GetByIdAsync(groupId, MemberIncludes)
            ?? throw new NotFoundException("Group not found.");

        var acting = group.Members.FirstOrDefault(m => m.UserId == currentUserId)
            ?? throw new ForbiddenException("Bu grubun üyesi değilsin.");
        if (acting.Role != GroupMemberRole.Admin)
            throw new ForbiddenException("Sadece yöneticiler rol değiştirebilir.");

        var target = group.Members.FirstOrDefault(m => m.UserId == targetUserId)
            ?? throw new NotFoundException("Bu kullanıcı grubun üyesi değil.");

        if (target.UserId == group.CreatedByUserId && newRole != GroupMemberRole.Admin)
            throw new BadRequestException("Grup sahibinin rütbesini indiremezsin, önce grubu devretmeli.");

        if (target.Role == GroupMemberRole.Admin && newRole != GroupMemberRole.Admin
            && group.Members.Count(m => m.Role == GroupMemberRole.Admin) == 1)
            throw new BadRequestException("Gruptaki tek yöneticiyi rütbesini indiremezsin.");

        target.Role = newRole;
        await _groups.SaveChangesAsync();

        await _notifications.CreateAsync(targetUserId, currentUserId, NotificationType.GroupRoleChanged, groupId: groupId);

        return await ToDtoAsync(group, currentUserId, includeMembers: true);
    }

    public async Task<GetGroupDto> TransferOwnershipAsync(Guid currentUserId, Guid groupId, Guid newOwnerUserId)
    {
        var group = await _groups.GetByIdAsync(groupId, MemberIncludes)
            ?? throw new NotFoundException("Group not found.");

        if (group.CreatedByUserId != currentUserId)
            throw new ForbiddenException("Sadece grubun sahibi bu grubu devredebilir.");

        if (newOwnerUserId == currentUserId)
            throw new BadRequestException("Grup zaten sende.");

        var newOwner = group.Members.FirstOrDefault(m => m.UserId == newOwnerUserId)
            ?? throw new BadRequestException("Devredilecek kişi grubun bir üyesi olmalı.");

        newOwner.Role = GroupMemberRole.Admin;
        group.CreatedByUserId = newOwnerUserId;
        await _groups.SaveChangesAsync();

        return await ToDtoAsync(group, currentUserId, includeMembers: true);
    }

    private static void EnsureAdmin(Group group, Guid currentUserId)
    {
        var isAdmin = group.Members.Any(m => m.UserId == currentUserId && m.Role == GroupMemberRole.Admin);
        if (!isAdmin)
            throw new ForbiddenException("Only a group admin can do this.");
    }

    private async Task<GetGroupDto> ToDtoAsync(Group group, Guid currentUserId, bool includeMembers)
    {
        var dto = _mapper.Map<GetGroupDto>(group);
        dto.IsCurrentUserMember = group.Members.Any(m => m.UserId == currentUserId);
        dto.IsCurrentUserAdmin = group.Members.Any(m => m.UserId == currentUserId && m.Role == GroupMemberRole.Admin);
        dto.IsCurrentUserModerator = group.Members.Any(m => m.UserId == currentUserId && m.Role == GroupMemberRole.Moderator);
        dto.IsCurrentUserOwner = group.CreatedByUserId == currentUserId;

        if (includeMembers && group.Members.Count > 0)
        {
            var summaries = await _users.GetSummariesAsync(group.Members.Select(m => m.UserId));
            dto.Members = group.Members
                .OrderByDescending(m => m.Role) // Admin, then Moderator, then Member
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
