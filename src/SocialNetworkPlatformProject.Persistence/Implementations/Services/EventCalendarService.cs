using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Events;
using SocialNetworkPlatformProject.Application.Interfaces.Services;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class EventCalendarService : IEventCalendarService
{
    private readonly IEventQueryService _queries;

    public EventCalendarService(IEventQueryService queries)
    {
        _queries = queries;
    }

    public async Task<CalendarFileDto> GetCalendarFileAsync(Guid currentUserId, Guid eventId, string? pageUrl)
    {
        // Going through the query service keeps every rule in one place: a stranger gets a 404 for a private or
        // group event, and the online link is only filled in for the organizer and people who are going.
        var ev = await _queries.GetByIdAsync(currentUserId, eventId);

        return new CalendarFileDto
        {
            FileName = IcsBuilder.BuildFileName(ev.Title),
            Content = IcsBuilder.Build(ev, pageUrl, DateTime.UtcNow)
        };
    }

    public async Task<CalendarLinkDto> GetGoogleCalendarLinkAsync(Guid currentUserId, Guid eventId, string? pageUrl)
    {
        var ev = await _queries.GetByIdAsync(currentUserId, eventId);
        return new CalendarLinkDto { Url = GoogleCalendarLinkBuilder.Build(ev, pageUrl) };
    }
}
