using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class StoryTagRepository : Repository<StoryTag>, IStoryTagRepository
{
    public StoryTagRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Dictionary<Guid, List<Guid>>> GetTaggedUserIdsAsync(IReadOnlyCollection<Guid> storyIds)
    {
        if (storyIds.Count == 0)
            return new Dictionary<Guid, List<Guid>>();

        var rows = await _dbSet.AsNoTracking()
            .Where(t => storyIds.Contains(t.StoryId))
            .OrderBy(t => t.CreatedAt)
            .Select(t => new { t.StoryId, t.TaggedUserId })
            .ToListAsync();

        return rows.GroupBy(r => r.StoryId).ToDictionary(g => g.Key, g => g.Select(r => r.TaggedUserId).ToList());
    }

    public async Task<HashSet<Guid>> GetStoryIdsTaggingAsync(Guid userId, IReadOnlyCollection<Guid> storyIds)
    {
        if (storyIds.Count == 0)
            return new HashSet<Guid>();

        var ids = await _dbSet.AsNoTracking()
            .Where(t => t.TaggedUserId == userId && storyIds.Contains(t.StoryId))
            .Select(t => t.StoryId)
            .ToListAsync();

        return ids.ToHashSet();
    }
}
