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
}
