using SocialNetworkPlatformProject.Application.DTOs.Groups;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IGroupService
{
    Task<GetGroupDto> CreateAsync(Guid currentUserId, PostGroupDto dto);

    Task<GetGroupDto> UpdateAsync(Guid currentUserId, Guid groupId, PutGroupDto dto);

    Task<GetGroupDto> GetByIdAsync(Guid currentUserId, Guid groupId);

    Task<List<GetGroupDto>> GetMyGroupsAsync(Guid currentUserId);

    Task<List<GetGroupDto>> DiscoverAsync(Guid currentUserId, string? search);

    Task<GetGroupDto> JoinAsync(Guid currentUserId, Guid groupId, bool acceptRules);

    Task<List<GetJoinRequestDto>> GetJoinRequestsAsync(Guid currentUserId, Guid groupId);

    Task ApproveJoinRequestAsync(Guid currentUserId, Guid groupId, Guid requesterUserId);

    Task RejectJoinRequestAsync(Guid currentUserId, Guid groupId, Guid requesterUserId);

    Task InviteMemberAsync(Guid currentUserId, Guid groupId, Guid targetUserId);

    Task<List<GetGroupInviteDto>> GetMyInvitesAsync(Guid currentUserId);

    Task AcceptInviteAsync(Guid currentUserId, Guid groupId, bool acceptRules);

    Task DeclineInviteAsync(Guid currentUserId, Guid groupId);

    Task LeaveAsync(Guid currentUserId, Guid groupId);

    Task DeleteAsync(Guid currentUserId, Guid groupId, bool asAdmin = false);

    Task RemoveMemberAsync(Guid currentUserId, Guid groupId, Guid targetUserId);

    Task<GetGroupDto> SetMemberRoleAsync(Guid currentUserId, Guid groupId, Guid targetUserId, string role);

    Task<GetGroupDto> TransferOwnershipAsync(Guid currentUserId, Guid groupId, Guid newOwnerUserId);

    Task<List<GetGroupRuleDto>> GetRulesAsync(Guid groupId);

    Task<List<GetGroupRuleDto>> SetRulesAsync(Guid currentUserId, Guid groupId, PutGroupRulesDto dto);

    Task<PagedResult<GetGroupMemberDto>> GetMembersAsync(Guid currentUserId, Guid groupId, string? search, string? role, int page, int pageSize);

    Task<GetGroupDto> SetMutedAsync(Guid currentUserId, Guid groupId, bool muted);
}
