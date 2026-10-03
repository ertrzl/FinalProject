using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Events;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Extensions;

namespace SocialNetworkPlatformProject.Controllers;

// event.html's discussion section (comments + organizer announcements)
[ApiController]
[Route("api/events/{eventId:guid}/comments")]
[Authorize]
public class EventCommentsController : ControllerBase
{
    private readonly IEventCommentService _comments;

    public EventCommentsController(IEventCommentService comments)
    {
        _comments = comments;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<GetEventCommentDto>>> Get(Guid eventId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        return await _comments.GetAsync(User.GetUserId(), eventId, page, pageSize);
    }

    [HttpPost]
    public async Task<ActionResult<GetEventCommentDto>> Create(Guid eventId, PostEventCommentDto dto)
    {
        var created = await _comments.CreateAsync(User.GetUserId(), eventId, dto);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpDelete("{commentId:guid}")]
    public async Task<IActionResult> Delete(Guid eventId, Guid commentId)
    {
        await _comments.DeleteAsync(User.GetUserId(), eventId, commentId);
        return NoContent();
    }
}
