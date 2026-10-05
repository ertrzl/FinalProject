using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    // Marks one token as used in a single atomic statement. False when somebody else already did (two requests
    // presenting the same token at once: only one of them wins).
    Task<bool> TryRevokeAsync(Guid id, DateTime now);

    // Revokes every token of the user that is still usable (password change, theft detected).
    Task RevokeAllAsync(Guid userId, DateTime now);

    // Removes the user's tokens that have expired anyway; keeps the table small.
    Task DeleteExpiredAsync(Guid userId, DateTime now);
}
