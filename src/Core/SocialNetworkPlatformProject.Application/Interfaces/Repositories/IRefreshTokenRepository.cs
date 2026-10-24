using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<bool> TryRevokeAsync(Guid id, DateTime now);

    Task RevokeAllAsync(Guid userId, DateTime now);

    Task DeleteExpiredAsync(Guid userId, DateTime now);

    Task<List<RefreshToken>> GetActiveAsync(Guid userId, DateTime now);

    Task<int> DeleteSessionAsync(Guid userId, Guid sessionId);

    Task<int> DeleteAllExpiredAsync(DateTime now);
}
