using SocialNetworkPlatformProject.Application.DTOs.Events;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// Turns an Event (with its Attendees loaded) into the DTO one particular viewer is allowed to see: their own
// status, whether they may see the online link, how full it is... Shared by the command and query services.
public interface IEventDtoBuilder
{
    // No extra lookups: what the event itself carries. For one row of a list, group names aside (see BuildManyAsync).
    GetEventDto Build(Event source, Guid viewerId);

    // A list of events: like Build, plus the group names in one batched lookup instead of one per event.
    Task<List<GetEventDto>> BuildManyAsync(IEnumerable<Event> events, Guid viewerId);

    // A single event for its own page: also the organizer, the group and a pending invite for this viewer.
    Task<GetEventDto> BuildDetailAsync(Event source, Guid viewerId);
}
