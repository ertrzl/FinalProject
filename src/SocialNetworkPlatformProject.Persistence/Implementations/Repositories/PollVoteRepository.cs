using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class PollVoteRepository : Repository<PollVote>, IPollVoteRepository
{
    public PollVoteRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Dictionary<Guid, int>> GetCountsByOptionAsync(IReadOnlyCollection<Guid> pollIds)
    {
        if (pollIds.Count == 0)
            return new Dictionary<Guid, int>();

        return await _dbSet.AsNoTracking()
            .Where(v => pollIds.Contains(v.PollId))
            .GroupBy(v => v.OptionId)
            .Select(g => new { OptionId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.OptionId, x => x.Count);
    }

    public async Task<Dictionary<Guid, Guid>> GetMineAsync(Guid userId, IReadOnlyCollection<Guid> pollIds)
    {
        if (pollIds.Count == 0)
            return new Dictionary<Guid, Guid>();

        return await _dbSet.AsNoTracking()
            .Where(v => v.UserId == userId && pollIds.Contains(v.PollId))
            .ToDictionaryAsync(v => v.PollId, v => v.OptionId);
    }
}
