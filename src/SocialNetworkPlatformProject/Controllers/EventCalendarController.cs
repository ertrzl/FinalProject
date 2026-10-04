using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialNetworkPlatformProject.Application.DTOs.Events;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Extensions;

namespace SocialNetworkPlatformProject.Controllers;

// event.html "Takvime Ekle": the event as an .ics file
[ApiController]
[Route("api/events")]
[Authorize]
public class EventCalendarController : ControllerBase
{
    private readonly IEventCalendarService _calendar;
    private readonly IConfiguration _configuration;

    public EventCalendarController(IEventCalendarService calendar, IConfiguration configuration)
    {
        _calendar = calendar;
        _configuration = configuration;
    }

    [HttpGet("{id:guid}/calendar")]
    public async Task<IActionResult> GetCalendarFile(Guid id)
    {
        var file = await _calendar.GetCalendarFileAsync(User.GetUserId(), id, PageUrl(id));
        return File(Encoding.UTF8.GetBytes(file.Content), "text/calendar; charset=utf-8", file.FileName);
    }

    [HttpGet("{id:guid}/calendar/google")]
    public async Task<ActionResult<CalendarLinkDto>> GetGoogleCalendarLink(Guid id)
    {
        return await _calendar.GetGoogleCalendarLinkAsync(User.GetUserId(), id, PageUrl(id));
    }

    // Behind a reverse proxy the request's own scheme/host are the internal ones, and the link ends up in
    // people's calendars for good, so a public address can be configured ("App:PublicBaseUrl").
    private string PageUrl(Guid eventId)
    {
        var configuredBase = _configuration["App:PublicBaseUrl"];
        var baseUrl = string.IsNullOrWhiteSpace(configuredBase) ? $"{Request.Scheme}://{Request.Host}" : configuredBase.TrimEnd('/');
        return $"{baseUrl}/event.html?id={eventId}";
    }
}
