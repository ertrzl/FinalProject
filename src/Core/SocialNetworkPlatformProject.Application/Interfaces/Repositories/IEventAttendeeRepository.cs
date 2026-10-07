using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IEventAttendeeRepository : IRepository<EventAttendee>
{
    // Attendance numbers for several events (and the viewer's place in each) in a few aggregate queries, without
    // loading the attendee rows. Events nobody has joined are simply missing from the result.
    Task<Dictionary<Guid, EventAttendance>> GetAttendanceAsync(IReadOnlyCollection<Guid> eventIds, Guid viewerId);
}
