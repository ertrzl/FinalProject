using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface ICommentLikeRepository : IRepository<CommentLike>
{
    // Entity-specific query methods will be added here as the CommentLike service needs them.
}
