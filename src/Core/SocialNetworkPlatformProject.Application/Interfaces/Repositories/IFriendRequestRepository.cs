using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IFriendRequestRepository : IRepository<FriendRequest>
{
    // Entity-specific query methods will be added here as the FriendRequest service needs them.
}
