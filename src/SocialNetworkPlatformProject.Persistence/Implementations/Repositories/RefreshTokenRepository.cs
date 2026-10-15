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

    public async Task<List<RefreshToken>> GetActiveAsync(Guid userId, DateTime now)
    {
        return await _dbSet.AsNoTracking()
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> DeleteSessionAsync(Guid userId, Guid sessionId)
    {
        return await _dbSet.Where(t => t.UserId == userId && t.SessionId == sessionId).ExecuteDeleteAsync();
    }

    public async Task<int> DeleteAllExpiredAsync(DateTime now)
    {
        return await _dbSet.Where(t => t.ExpiresAt < now).ExecuteDeleteAsync();
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
