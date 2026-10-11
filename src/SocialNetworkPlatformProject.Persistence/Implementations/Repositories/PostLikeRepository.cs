using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class PostLikeRepository : Repository<PostLike>, IPostLikeRepository
{
    public PostLikeRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Dictionary<Guid, Dictionary<ReactionType, int>>> GetReactionCountsAsync(IReadOnlyCollection<Guid> postIds)
    {
        if (postIds.Count == 0)
            return new Dictionary<Guid, Dictionary<ReactionType, int>>();

        var rows = await _dbSet.AsNoTracking()
            .Where(l => postIds.Contains(l.PostId))
            .GroupBy(l => new { l.PostId, l.Type })
            .Select(g => new { g.Key.PostId, g.Key.Type, Count = g.Count() })
            .ToListAsync();

        return rows
            .GroupBy(r => r.PostId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(r => r.Type, r => r.Count));
    }

    public async Task<Dictionary<Guid, ReactionType>> GetMineAsync(Guid userId, IReadOnlyCollection<Guid> postIds)
    {
        if (postIds.Count == 0)
            return new Dictionary<Guid, ReactionType>();

        return await _dbSet.AsNoTracking()
            .Where(l => l.UserId == userId && postIds.Contains(l.PostId))
            .ToDictionaryAsync(l => l.PostId, l => l.Type);
    }

    public async Task<List<PostLike>> GetNewestAsync(Guid postId, ReactionType? type, int take)
    {
        return await _dbSet.AsNoTracking()
            .Where(l => l.PostId == postId && (type == null || l.Type == type))
            .OrderByDescending(l => l.CreatedAt)
            .ThenBy(l => l.Id)
            .Take(take)
            .ToListAsync();
    }
}
