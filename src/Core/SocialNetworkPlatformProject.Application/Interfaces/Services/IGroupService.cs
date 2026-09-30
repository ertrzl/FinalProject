using SocialNetworkPlatformProject.Application.DTOs.Groups;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IGroupService
{
    Task<GetGroupDto> CreateAsync(Guid currentUserId, PostGroupDto dto);

    Task<GetGroupDto> GetByIdAsync(Guid currentUserId, Guid groupId);

    // groups.html "Gruplarım" tab.
    Task<List<GetGroupDto>> GetMyGroupsAsync(Guid currentUserId);

    // groups.html "Keşfet" tab: public groups the user hasn't joined yet.
    Task<List<GetGroupDto>> DiscoverAsync(Guid currentUserId, string? search);

    Task<GetGroupDto> JoinAsync(Guid currentUserId, Guid groupId);

    Task LeaveAsync(Guid currentUserId, Guid groupId);

    Task DeleteAsync(Guid currentUserId, Guid groupId);

    // Admin/moderator kicks a member out. Admins can't be kicked; moderators can't kick other moderators.
    Task RemoveMemberAsync(Guid currentUserId, Guid groupId, Guid targetUserId);

    // Admin-only: promote/demote a member's role. Blocked on the sole admin and on the group owner.
    Task<GetGroupDto> SetMemberRoleAsync(Guid currentUserId, Guid groupId, Guid targetUserId, string role);

    // Owner-only: hands group ownership to another member (who becomes Admin), so the owner can then leave.
    Task<GetGroupDto> TransferOwnershipAsync(Guid currentUserId, Guid groupId, Guid newOwnerUserId);
}
