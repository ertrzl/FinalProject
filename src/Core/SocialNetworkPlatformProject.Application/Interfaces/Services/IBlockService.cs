using SocialNetworkPlatformProject.Application.DTOs.Users;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// Blocking: the blocker lifts it, but the two people are hidden from each other while it lasts.
public interface IBlockService
{
    // Also ends the friendship, cancels pending friend requests and clears the notifications between the two.
    Task BlockAsync(Guid currentUserId, Guid userId);

    Task UnblockAsync(Guid currentUserId, Guid userId);

    Task<List<GetBlockedUserDto>> GetBlockedAsync(Guid currentUserId);

    // Either of the two blocked the other.
    Task<bool> AreBlockedAsync(Guid userA, Guid userB);

    // The first blocked the second.
    Task<bool> HasBlockedAsync(Guid blockerId, Guid blockedId);

    // Everybody the user blocked plus everybody who blocked the user: the people hidden from them.
    Task<HashSet<Guid>> GetBlockedIdsAsync(Guid userId);
}
