using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IStoryRepository : IRepository<Story>
{
    // Entity-specific query methods will be added here as the Story service needs them.
}
