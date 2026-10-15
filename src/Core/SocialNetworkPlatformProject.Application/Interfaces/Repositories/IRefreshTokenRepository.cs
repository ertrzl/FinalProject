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

    // The tokens of the user that can still be used (not revoked, not expired), newest first: one per open session.
    Task<List<RefreshToken>> GetActiveAsync(Guid userId, DateTime now);

    // Deletes every token row of one session of the user, used or not; returns how many (0 = no such session). Deleted, not
    // revoked: a device that comes back with one of these tokens is simply refused. A *revoked* token coming back is taken for
    // a stolen one and ends all the user's sessions (see SessionIssuer.RotateAsync), which is wrong for a session the person
    // ended on purpose.
    Task<int> DeleteSessionAsync(Guid userId, Guid sessionId);

    // The same for everybody's tokens (the timer's clean-up); returns how many went.
    Task<int> DeleteAllExpiredAsync(DateTime now);
}
