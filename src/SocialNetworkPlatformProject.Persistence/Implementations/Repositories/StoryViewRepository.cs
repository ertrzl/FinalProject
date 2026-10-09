using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class StoryViewRepository : Repository<StoryView>, IStoryViewRepository
{
    public StoryViewRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Dictionary<Guid, int>> CountByStoryAsync(IReadOnlyCollection<Guid> storyIds)
    {
        if (storyIds.Count == 0)
            return new Dictionary<Guid, int>();

        return await _dbSet.AsNoTracking()
            .Where(v => storyIds.Contains(v.StoryId))
            .GroupBy(v => v.StoryId)
            .Select(g => new { StoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.StoryId, x => x.Count);
    }

    public async Task<HashSet<Guid>> GetViewedStoryIdsAsync(Guid viewerId, IReadOnlyCollection<Guid> storyIds)
    {
        if (storyIds.Count == 0)
            return new HashSet<Guid>();

        var ids = await _dbSet.AsNoTracking()
            .Where(v => v.ViewerId == viewerId && storyIds.Contains(v.StoryId))
            .Select(v => v.StoryId)
            .ToListAsync();

        return ids.ToHashSet();
    }

    public async Task<List<StoryView>> GetNewestAsync(Guid storyId, int take)
    {
        return await _dbSet.AsNoTracking()
            .Where(v => v.StoryId == storyId)
            .OrderByDescending(v => v.CreatedAt)
            .ThenBy(v => v.Id)
            .Take(take)
            .ToListAsync();
    }
}
