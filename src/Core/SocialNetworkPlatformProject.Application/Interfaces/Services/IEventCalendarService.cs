using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IEventCalendarService
{
    Task<CalendarFileDto> GetCalendarFileAsync(Guid currentUserId, Guid eventId, string? pageUrl);

    Task<CalendarLinkDto> GetGoogleCalendarLinkAsync(Guid currentUserId, Guid eventId, string? pageUrl);
}
