using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class CommentLikeRepository : Repository<CommentLike>, ICommentLikeRepository
{
    public CommentLikeRepository(ApplicationDbContext context) : base(context)
    {
    }
}
