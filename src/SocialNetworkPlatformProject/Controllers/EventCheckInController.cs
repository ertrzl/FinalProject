using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialNetworkPlatformProject.Application.DTOs.Events;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Extensions;

namespace SocialNetworkPlatformProject.Controllers;

[ApiController]
[Route("api/events/{eventId:guid}/attendance")]
[Authorize]
public class EventCheckInController : ControllerBase
{
    private readonly IEventCheckInService _checkIn;

    public EventCheckInController(IEventCheckInService checkIn)
    {
        _checkIn = checkIn;
    }

    [HttpGet]
    public async Task<ActionResult<GetEventAttendanceDto>> Get(Guid eventId)
    {
        return await _checkIn.GetAsync(User.GetUserId(), eventId);
    }

    [HttpPut("{userId:guid}")]
    public async Task<ActionResult<GetEventAttendanceDto>> Set(Guid eventId, Guid userId, PutEventCheckInDto dto)
    {
        return await _checkIn.SetAsync(User.GetUserId(), eventId, userId, dto);
    }
}
