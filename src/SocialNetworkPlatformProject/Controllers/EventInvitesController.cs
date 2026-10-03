using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialNetworkPlatformProject.Application.DTOs.Events;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Extensions;

namespace SocialNetworkPlatformProject.Controllers;

// events.html "Davetler" tab + event.html "Davet Et" modal
[ApiController]
[Route("api/events")]
[Authorize]
public class EventInvitesController : ControllerBase
{
    private readonly IEventInviteService _invites;

    public EventInvitesController(IEventInviteService invites)
    {
        _invites = invites;
    }

    [HttpGet("invites/mine")]
    public async Task<ActionResult<List<GetEventInviteDto>>> GetMine()
    {
        return await _invites.GetMyInvitesAsync(User.GetUserId());
    }

    [HttpGet("{id:guid}/invites")]
    public async Task<ActionResult<List<GetEventInviteeDto>>> GetInvitees(Guid id)
    {
        return await _invites.GetInviteesAsync(User.GetUserId(), id);
    }

    [HttpPost("{id:guid}/invites")]
    public async Task<IActionResult> Invite(Guid id, PostEventInviteDto dto)
    {
        await _invites.InviteAsync(User.GetUserId(), id, dto);
        return NoContent();
    }

    [HttpPost("{id:guid}/invites/accept")]
    public async Task<ActionResult<GetEventDto>> Accept(Guid id)
    {
        return await _invites.AcceptAsync(User.GetUserId(), id);
    }

    [HttpPost("{id:guid}/invites/decline")]
    public async Task<IActionResult> Decline(Guid id)
    {
        await _invites.DeclineAsync(User.GetUserId(), id);
        return NoContent();
    }
}
