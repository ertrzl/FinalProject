using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IPostLikeRepository : IRepository<PostLike>
{
    // Entity-specific query methods will be added here as the PostLike service needs them.
}
