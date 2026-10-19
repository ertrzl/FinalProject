using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.DTOs.Games;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class GameRepository : Repository<Game>, IGameRepository
{
    public GameRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<int> ExpireStaleForPairAsync(string pairKey, DateTime invitedBefore, DateTime idleSince, DateTime now)
    {
        return await _dbSet
            .Where(g => g.PairKey == pairKey
                && ((g.Status == GameStatus.Invited && g.CreatedAt < invitedBefore)
                    || (g.Status == GameStatus.Active && g.UpdatedAt < idleSince)))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(g => g.Status, GameStatus.Expired)
                .SetProperty(g => g.UpdatedAt, now));
    }

    public async Task ReloadAsync(Game game)
    {
        await _context.Entry(game).ReloadAsync();
    }

    public async Task<List<Game>> GetIncomingInvitesAsync(Guid userId, DateTime createdAfter)
    {
        return await _dbSet.AsNoTracking()
            .Where(g => g.Status == GameStatus.Invited && g.InviteeId == userId && g.CreatedAt >= createdAfter)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Game>> GetSentInvitesAsync(Guid userId, DateTime createdAfter)
    {
        return await _dbSet.AsNoTracking()
            .Where(g => g.Status == GameStatus.Invited && g.InviterId == userId && g.CreatedAt >= createdAfter)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Game>> GetActiveAsync(Guid userId, DateTime updatedAfter)
    {
        return await _dbSet.AsNoTracking()
            .Where(g => g.Status == GameStatus.Active && (g.InviterId == userId || g.InviteeId == userId) && g.UpdatedAt >= updatedAfter)
            .OrderByDescending(g => g.UpdatedAt)
            .ToListAsync();
    }

    public async Task<List<Game>> GetRecentFinishedAsync(Guid userId, int take)
    {
        return await _dbSet.AsNoTracking()
            .Where(g => g.Status == GameStatus.Finished && (g.InviterId == userId || g.InviteeId == userId))
            .OrderByDescending(g => g.FinishedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<GetGameStatsDto> GetStatsAsync(Guid userId)
    {
        var finished = _dbSet.AsNoTracking()
            .Where(g => g.Status == GameStatus.Finished && (g.InviterId == userId || g.InviteeId == userId));

        // A loss is a game somebody else won, by three in a row or because I gave up.
        return new GetGameStatsDto
        {
            Wins = await finished.CountAsync(g => g.WinnerId == userId),
            Losses = await finished.CountAsync(g => g.Outcome != GameOutcome.Draw && g.WinnerId != userId),
            Draws = await finished.CountAsync(g => g.Outcome == GameOutcome.Draw)
        };
    }
}
