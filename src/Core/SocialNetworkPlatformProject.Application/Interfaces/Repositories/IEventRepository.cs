using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IEventRepository : IRepository<Event>
{
    // Entity-specific query methods will be added here as the Event service needs them.
}
