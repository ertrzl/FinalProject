using SocialNetworkPlatformProject.Application.DTOs.Users;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IBlockService
{
    Task BlockAsync(Guid currentUserId, Guid userId);

    Task UnblockAsync(Guid currentUserId, Guid userId);

    Task<List<GetBlockedUserDto>> GetBlockedAsync(Guid currentUserId);

    Task<bool> AreBlockedAsync(Guid userA, Guid userB);

    Task<bool> HasBlockedAsync(Guid blockerId, Guid blockedId);

    Task<HashSet<Guid>> GetBlockedIdsAsync(Guid userId);
}
