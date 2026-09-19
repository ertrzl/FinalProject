using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IPostRepository : IRepository<Post>
{
    // Entity-specific query methods will be added here as the Post service needs them.
}
