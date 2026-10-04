using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// The read side of events: everything that lists or fetches events without changing them.
public interface IEventQueryService
{
    Task<GetEventDto> GetByIdAsync(Guid currentUserId, Guid eventId);

    // Everyone going or interested (optionally just one status), going first. Capped; the counts on the event stay exact.
    Task<List<GetEventAttendeeDto>> GetAttendeesAsync(Guid currentUserId, Guid eventId, string? status);

    // Events that haven't finished (ongoing ones included), soonest first, filtered and paged.
    Task<PagedResult<GetEventDto>> GetUpcomingAsync(Guid currentUserId, EventQuery query);

    // Upcoming events of one group. Members only (ForbiddenException otherwise).
    Task<List<GetEventDto>> GetGroupEventsAsync(Guid currentUserId, Guid groupId);

    // "Etkinliklerim" lists, see EventMineScope.
    Task<List<GetEventDto>> GetMineAsync(Guid currentUserId, string scope);
}
