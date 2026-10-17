using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class StoryLikeRepository : Repository<StoryLike>, IStoryLikeRepository
{
    public StoryLikeRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Dictionary<Guid, int>> CountByStoryAsync(IReadOnlyCollection<Guid> storyIds)
    {
        if (storyIds.Count == 0)
            return new Dictionary<Guid, int>();

        return await _dbSet.AsNoTracking()
            .Where(l => storyIds.Contains(l.StoryId))
            .GroupBy(l => l.StoryId)
            .Select(g => new { StoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.StoryId, x => x.Count);
    }

    public async Task<HashSet<Guid>> GetLikedStoryIdsAsync(Guid userId, IReadOnlyCollection<Guid> storyIds)
    {
        if (storyIds.Count == 0)
            return new HashSet<Guid>();

        var ids = await _dbSet.AsNoTracking()
            .Where(l => l.UserId == userId && storyIds.Contains(l.StoryId))
            .Select(l => l.StoryId)
            .ToListAsync();

        return ids.ToHashSet();
    }

    public async Task<HashSet<Guid>> GetLikerIdsAsync(Guid storyId)
    {
        var ids = await _dbSet.AsNoTracking()
            .Where(l => l.StoryId == storyId)
            .Select(l => l.UserId)
            .ToListAsync();

        return ids.ToHashSet();
    }
}
