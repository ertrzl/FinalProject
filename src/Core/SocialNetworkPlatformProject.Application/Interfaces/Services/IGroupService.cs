using SocialNetworkPlatformProject.Application.DTOs.Groups;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IGroupService
{
    Task<GetGroupDto> CreateAsync(Guid currentUserId, PostGroupDto dto);

    // Admin-only: rename/redescribe/re-privacy/re-cover an existing group.
    Task<GetGroupDto> UpdateAsync(Guid currentUserId, Guid groupId, PutGroupDto dto);

    Task<GetGroupDto> GetByIdAsync(Guid currentUserId, Guid groupId);

    // groups.html "Gruplarım" tab.
    Task<List<GetGroupDto>> GetMyGroupsAsync(Guid currentUserId);

    // groups.html "Keşfet" tab: groups (public or private) the user hasn't joined yet.
    // Private ones show a "request to join" action instead of an immediate join.
    Task<List<GetGroupDto>> DiscoverAsync(Guid currentUserId, string? search);

    // Public groups: joins immediately. Private groups: files a join request instead (see GroupJoinRequest).
    Task<GetGroupDto> JoinAsync(Guid currentUserId, Guid groupId);

    // Admin-only: list of users waiting to be let into a private group.
    Task<List<GetJoinRequestDto>> GetJoinRequestsAsync(Guid currentUserId, Guid groupId);

    // Admin-only: turns a join request into membership.
    Task ApproveJoinRequestAsync(Guid currentUserId, Guid groupId, Guid requesterUserId);

    // Admin-only: discards a join request.
    Task RejectJoinRequestAsync(Guid currentUserId, Guid groupId, Guid requesterUserId);

    // Admin-only: invites any user — they must accept it themselves, this never adds them directly.
    // If that user already has a pending join request, invite and request are mutual intent, so it's
    // folded straight into membership instead of creating a redundant parallel invite.
    Task InviteMemberAsync(Guid currentUserId, Guid groupId, Guid targetUserId);

    // groups.html "Davetler" tab: invites waiting for the current user's own response.
    Task<List<GetGroupInviteDto>> GetMyInvitesAsync(Guid currentUserId);

    Task AcceptInviteAsync(Guid currentUserId, Guid groupId);

    Task DeclineInviteAsync(Guid currentUserId, Guid groupId);

    Task LeaveAsync(Guid currentUserId, Guid groupId);

    Task DeleteAsync(Guid currentUserId, Guid groupId);

    // Admin/moderator kicks a member out. Admins can't be kicked; moderators can't kick other moderators.
    Task RemoveMemberAsync(Guid currentUserId, Guid groupId, Guid targetUserId);

    // Admin-only: promote/demote a member's role. Blocked on the sole admin and on the group owner.
    Task<GetGroupDto> SetMemberRoleAsync(Guid currentUserId, Guid groupId, Guid targetUserId, string role);

    // Owner-only: hands group ownership to another member (who becomes Admin), so the owner can then leave.
    Task<GetGroupDto> TransferOwnershipAsync(Guid currentUserId, Guid groupId, Guid newOwnerUserId);
}
