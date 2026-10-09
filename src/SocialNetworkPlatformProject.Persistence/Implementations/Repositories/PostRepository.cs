using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class PostRepository : Repository<Post>, IPostRepository
{
    public PostRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Dictionary<Guid, PostCounts>> GetCountsAsync(IReadOnlyCollection<Guid> postIds)
    {
        var ids = postIds.Distinct().ToList();

        var likes = await _context.Set<PostLike>()
            .Where(l => ids.Contains(l.PostId))
            .GroupBy(l => l.PostId)
            .Select(g => new { PostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PostId, x => x.Count);

        var comments = await _context.Set<Comment>()
            .Where(c => ids.Contains(c.PostId))
            .GroupBy(c => c.PostId)
            .Select(g => new { PostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PostId, x => x.Count);

        return ids.ToDictionary(id => id, id => new PostCounts(likes.GetValueOrDefault(id), comments.GetValueOrDefault(id)));
    }

    public async Task<Dictionary<Guid, int>> GetShareCountsAsync(IReadOnlyCollection<Guid> postIds)
    {
        if (postIds.Count == 0)
            return new Dictionary<Guid, int>();

        return await _dbSet.AsNoTracking()
            .Where(p => p.SharedPostId != null && postIds.Contains(p.SharedPostId.Value))
            .GroupBy(p => p.SharedPostId!.Value)
            .Select(g => new { PostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PostId, x => x.Count);
    }

    public async Task DetachSharesAsync(IReadOnlyCollection<Guid> postIds)
    {
        if (postIds.Count == 0)
            return;

        await _dbSet
            .Where(p => p.SharedPostId != null && postIds.Contains(p.SharedPostId.Value))
            .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.SharedPostId, (Guid?)null));
    }
}
