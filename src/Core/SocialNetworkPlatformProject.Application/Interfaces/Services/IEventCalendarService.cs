using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IEventCalendarService
{
    // An .ics file for an event the viewer can see; the online link is included only if they may see it.
    // pageUrl is the absolute address of the event's page (the web layer knows the host), or null to leave it out.
    Task<CalendarFileDto> GetCalendarFileAsync(Guid currentUserId, Guid eventId, string? pageUrl);

    // Google Calendar's pre-filled "create event" address, with the same visibility rules as the file.
    Task<CalendarLinkDto> GetGoogleCalendarLinkAsync(Guid currentUserId, Guid eventId, string? pageUrl);
}
