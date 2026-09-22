using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    public EventsController(IEventService events)
    {
        _events = events;
    }

    [HttpGet("upcoming")]
    public async Task<ActionResult<List<GetEventDto>>> GetUpcoming()
    {
        return await _events.GetUpcomingAsync(User.GetUserId());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetEventDto>> GetById(Guid id)
    {
        return await _events.GetByIdAsync(User.GetUserId(), id);
    }

    [HttpPost]
    public async Task<ActionResult<GetEventDto>> Create([FromForm] PostEventDto dto)
    {
        var newEvent = await _events.CreateAsync(User.GetUserId(), dto);
        return CreatedAtAction(nameof(GetById), new { id = newEvent.Id }, newEvent);
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
