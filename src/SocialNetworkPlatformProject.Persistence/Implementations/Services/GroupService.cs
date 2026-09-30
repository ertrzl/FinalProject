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

    private readonly IGroupRepository _groups;
    private readonly IGroupMemberRepository _members;
    private readonly IUserRepository _users;
    private readonly IFileStorageService _files;
    private readonly IMapper _mapper;

    public GroupService(
        IGroupRepository groups,
        IGroupMemberRepository members,
        IUserRepository users,
        IFileStorageService files,
        IMapper mapper)
    {
        _groups = groups;
        _members = members;
        _users = users;
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

        var groups = await _groups.GetAll(
                filter: g => g.Privacy == GroupPrivacy.Public
                             && g.Members.All(m => m.UserId != currentUserId)
                             && (term == null || g.Name.Contains(term)),
                orderBy: g => g.CreatedAt,
                isDescending: true,
                asNoTracking: true,
                includes: MemberIncludes)
            .ToListAsync();

        return (await Task.WhenAll(groups.Select(g => ToDtoAsync(g, currentUserId, includeMembers: false)))).ToList();
    }

    public async Task<GetGroupDto> JoinAsync(Guid currentUserId, Guid groupId)
    {
        var group = await _groups.GetByIdAsync(groupId, MemberIncludes)
            ?? throw new NotFoundException("Group not found.");

        if (group.Privacy == GroupPrivacy.Private)
            throw new ForbiddenException("This group is private.");

        if (group.Members.Any(m => m.UserId == currentUserId))
            throw new ConflictException("You are already a member of this group.");

        var member = new GroupMember { GroupId = groupId, UserId = currentUserId, Role = GroupMemberRole.Member };
        await _members.AddAsync(member);
        await _members.SaveChangesAsync();

        // The tracked Members collection was fixed up by EF when the new row was added.
        return await ToDtoAsync(group, currentUserId, includeMembers: false);
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
            _groups.Delete(group);
            await _groups.SaveChangesAsync();
            _files.Delete(group.CoverImageUrl);
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
    }

    public async Task DeleteAsync(Guid currentUserId, Guid groupId)
    {
        var group = await _groups.GetByIdAsync(groupId, MemberIncludes)
            ?? throw new NotFoundException("Group not found.");

        var isAdmin = group.Members.Any(m => m.UserId == currentUserId && m.Role == GroupMemberRole.Admin);
        if (!isAdmin)
            throw new ForbiddenException("Only a group admin can delete the group.");

        _groups.Delete(group);
        await _groups.SaveChangesAsync();

        _files.Delete(group.CoverImageUrl);
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
