using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class HashtagRepository : Repository<Hashtag>, IHashtagRepository
{
    public HashtagRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task LinkAsync(Post post, Hashtag hashtag)
    {
        await _context.Set<PostHashtag>().AddAsync(new PostHashtag { Post = post, Hashtag = hashtag });
    }

    public async Task<int> DeleteUnusedCreatedBeforeAsync(DateTime limitUtc)
    {
        return await _dbSet.Where(h => h.CreatedAt < limitUtc && !h.Posts.Any()).ExecuteDeleteAsync();
    }
}
