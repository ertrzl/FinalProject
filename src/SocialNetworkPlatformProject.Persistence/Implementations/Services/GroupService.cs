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
    private readonly IFileStorageService _files;
    private readonly IMapper _mapper;

    public GroupService(
        IGroupRepository groups,
        IGroupMemberRepository members,
        IFileStorageService files,
        IMapper mapper)
    {
        _groups = groups;
        _members = members;
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

        return ToDto(group, currentUserId);
    }

    public async Task<GetGroupDto> GetByIdAsync(Guid currentUserId, Guid groupId)
    {
        var group = await _groups.GetByIdAsync(groupId, MemberIncludes)
            ?? throw new NotFoundException("Group not found.");

        // Private groups behave as if they don't exist for non-members.
        if (group.Privacy == GroupPrivacy.Private && group.Members.All(m => m.UserId != currentUserId))
            throw new NotFoundException("Group not found.");

        return ToDto(group, currentUserId);
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

        return groups.Select(g => ToDto(g, currentUserId)).ToList();
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

        return groups.Select(g => ToDto(g, currentUserId)).ToList();
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
        return ToDto(group, currentUserId);
    }

    public async Task LeaveAsync(Guid currentUserId, Guid groupId)
    {
        var group = await _groups.GetByIdAsync(groupId, MemberIncludes)
            ?? throw new NotFoundException("Group not found.");

        var membership = group.Members.FirstOrDefault(m => m.UserId == currentUserId)
            ?? throw new BadRequestException("You are not a member of this group.");

        var others = group.Members.Where(m => m.UserId != currentUserId).OrderBy(m => m.CreatedAt).ToList();

        if (others.Count == 0)
        {
            // Last member out: nothing left to keep.
            _groups.Delete(group);
            await _groups.SaveChangesAsync();
            _files.Delete(group.CoverImageUrl);
            return;
        }

        // Never leave a group without an admin: hand the role to its longest-standing member.
        if (membership.Role == GroupMemberRole.Admin && others.All(m => m.Role != GroupMemberRole.Admin))
            others[0].Role = GroupMemberRole.Admin;

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

    private GetGroupDto ToDto(Group group, Guid currentUserId)
    {
        var dto = _mapper.Map<GetGroupDto>(group);
        dto.IsCurrentUserMember = group.Members.Any(m => m.UserId == currentUserId);
        return dto;
    }
}
