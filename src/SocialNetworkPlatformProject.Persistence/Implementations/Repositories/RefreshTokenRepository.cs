using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<bool> TryRevokeAsync(Guid id, DateTime now)
    {
        var changed = await _context.Set<RefreshToken>()
            .Where(t => t.Id == id && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now));

        return changed == 1;
    }

    public async Task RevokeAllAsync(Guid userId, DateTime now)
    {
        await _context.Set<RefreshToken>()
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now));
    }

    public async Task DeleteExpiredAsync(Guid userId, DateTime now)
    {
        await _context.Set<RefreshToken>()
            .Where(t => t.UserId == userId && t.ExpiresAt < now)
            .ExecuteDeleteAsync();
    }
}
