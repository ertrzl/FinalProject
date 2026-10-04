using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Events;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Extensions;

namespace SocialNetworkPlatformProject.Controllers;

// events.html cards + "Katılıyorum" / "İlgileniyorum" buttons
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EventsController : ControllerBase
{
    private readonly IEventService _events;
    private readonly IEventQueryService _queries;

    public EventsController(IEventService events, IEventQueryService queries)
    {
        _events = events;
        _queries = queries;
    }

    // events.html "Yaklaşan": search box and filter chips, paged
    [HttpGet("upcoming")]
    public async Task<ActionResult<PagedResult<GetEventDto>>> GetUpcoming([FromQuery] EventQuery query)
    {
        return await _queries.GetUpcomingAsync(User.GetUserId(), query);
    }

    // group.html's "Etkinlikler" tab: upcoming events of one group (members only)
    [HttpGet("group/{groupId:guid}")]
    public async Task<ActionResult<List<GetEventDto>>> GetGroupEvents(Guid groupId)
    {
        return await _queries.GetGroupEventsAsync(User.GetUserId(), groupId);
    }

    // scope = "created", "past" or "attending" (see EventMineScope)
    [HttpGet("mine")]
    public async Task<ActionResult<List<GetEventDto>>> GetMine([FromQuery] string scope = EventMineScope.Created)
    {
        return await _queries.GetMineAsync(User.GetUserId(), scope);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetEventDto>> GetById(Guid id)
    {
        return await _queries.GetByIdAsync(User.GetUserId(), id);
    }

    // ?status=Going or ?status=Interested narrows the list; without it everyone is returned.
    [HttpGet("{id:guid}/attendees")]
    public async Task<ActionResult<List<GetEventAttendeeDto>>> GetAttendees(Guid id, [FromQuery] string? status = null)
    {
        return await _queries.GetAttendeesAsync(User.GetUserId(), id, status);
    }

    [HttpPost]
    public async Task<ActionResult<GetEventDto>> Create([FromForm] PostEventDto dto)
    {
        var newEvent = await _events.CreateAsync(User.GetUserId(), dto);
        return CreatedAtAction(nameof(GetById), new { id = newEvent.Id }, newEvent);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<GetEventDto>> Update(Guid id, [FromForm] PutEventDto dto)
    {
        return await _events.UpdateAsync(User.GetUserId(), id, dto);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<GetEventDto>> SetStatus(Guid id, PutEventStatusDto dto)
    {
        return await _events.SetStatusAsync(User.GetUserId(), id, dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _events.DeleteAsync(User.GetUserId(), id);
        return NoContent();
    }
}
