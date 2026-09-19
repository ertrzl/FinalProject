using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IEventAttendeeRepository : IRepository<EventAttendee>
{
    // Entity-specific query methods will be added here as the EventAttendee service needs them.
}
