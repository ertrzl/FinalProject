using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class PostMentionRepository : Repository<PostMention>, IPostMentionRepository
{
    public PostMentionRepository(ApplicationDbContext context) : base(context)
    {
    }

    public IQueryable<Guid> PostIdsMentioning(Guid userId)
    {
        return _dbSet.Where(m => m.MentionedUserId == userId).Select(m => m.PostId);
    }
}
