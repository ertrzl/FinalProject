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
        // AddAsync marks the row as new whatever its Id looks like; the navigations put it into post.Hashtags as well.
        await _context.Set<PostHashtag>().AddAsync(new PostHashtag { Post = post, Hashtag = hashtag });
    }
}
