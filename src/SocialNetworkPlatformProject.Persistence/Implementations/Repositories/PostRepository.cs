using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;
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

    public IQueryable<Post> VisibleTo(IQueryable<Post> posts, Guid viewerId)
    {
        var friendships = _context.Set<Friendship>();
        var users = _context.Users;
        var memberships = _context.Set<GroupMember>();
        var blocks = _context.Set<UserBlock>();

        return posts
            .Where(p => !blocks.Any(b => (b.BlockerId == viewerId && b.BlockedId == p.AuthorId) || (b.BlockerId == p.AuthorId && b.BlockedId == viewerId)))
            .Where(p => p.GroupId == null
            ? p.AuthorId == viewerId
                || friendships.Any(f => (f.UserOneId == viewerId && f.UserTwoId == p.AuthorId) || (f.UserTwoId == viewerId && f.UserOneId == p.AuthorId))
                || (p.Privacy == PostPrivacy.Public && users.Any(u => u.Id == p.AuthorId && !u.IsPrivateAccount))
            : p.Group!.Privacy == GroupPrivacy.Public
                || memberships.Any(m => m.GroupId == p.GroupId && m.UserId == viewerId));
    }

    public async Task<Post?> FindByMediaUrlAsync(string url)
    {
        return await _context.Set<PostMedia>().AsNoTracking()
            .Where(m => m.Url == url)
            .Select(m => m.Post)
            .FirstOrDefaultAsync();
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
