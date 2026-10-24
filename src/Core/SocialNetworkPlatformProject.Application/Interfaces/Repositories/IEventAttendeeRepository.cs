using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IEventAttendeeRepository : IRepository<EventAttendee>
{
    Task<Dictionary<Guid, EventAttendance>> GetAttendanceAsync(IReadOnlyCollection<Guid> eventIds, Guid viewerId);
}
