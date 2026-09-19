using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface ISavedPostRepository : IRepository<SavedPost>
{
    // Entity-specific query methods will be added here as the SavedPost service needs them.
}
