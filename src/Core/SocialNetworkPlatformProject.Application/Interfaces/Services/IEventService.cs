using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IEventService
{
    Task<GetEventDto> CreateAsync(Guid currentUserId, PostEventDto dto);

    Task<GetEventDto> GetByIdAsync(Guid currentUserId, Guid eventId);

    // Events that haven't started yet, soonest first.
    Task<List<GetEventDto>> GetUpcomingAsync(Guid currentUserId);

    // Everyone going or interested (optionally just one status), going first. Capped at EventService's MaxAttendeesListed.
    Task<List<GetEventAttendeeDto>> GetAttendeesAsync(Guid currentUserId, Guid eventId, string? status);

    // Upcoming events of one group. Members only (ForbiddenException otherwise).
    Task<List<GetEventDto>> GetGroupEventsAsync(Guid currentUserId, Guid groupId);

    // "created": events I organized (any date); "past": finished events I was going to / interested in. Newest first.
    Task<List<GetEventDto>> GetMineAsync(Guid currentUserId, string scope);

    // Creator-only, and only until the event starts. Notifies the people going/interested when the title, time or place changed.
    Task<GetEventDto> UpdateAsync(Guid currentUserId, Guid eventId, PutEventDto dto);

    Task<GetEventDto> SetStatusAsync(Guid currentUserId, Guid eventId, PutEventStatusDto dto);

    Task DeleteAsync(Guid currentUserId, Guid eventId);
}
