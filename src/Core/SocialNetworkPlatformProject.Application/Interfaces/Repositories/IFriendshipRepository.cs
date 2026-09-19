using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IFriendshipRepository : IRepository<Friendship>
{
    // Entity-specific query methods will be added here as the Friendship service needs them.
}
