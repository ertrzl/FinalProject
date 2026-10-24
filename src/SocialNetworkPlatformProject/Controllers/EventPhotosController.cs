using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialNetworkPlatformProject.Application.DTOs.Events;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Extensions;

namespace SocialNetworkPlatformProject.Controllers;

[ApiController]
[Route("api/events/{eventId:guid}/photos")]
[Authorize]
public class EventPhotosController : ControllerBase
{
    private readonly IEventPhotoService _photos;

    public EventPhotosController(IEventPhotoService photos)
    {
        _photos = photos;
    }

    [HttpGet]
    public async Task<ActionResult<GetEventPhotosDto>> Get(Guid eventId)
    {
        return await _photos.GetAsync(User.GetUserId(), eventId);
    }

    [HttpPost]
    [RequestSizeLimit(UploadLimits.MaxRequestBytes)]
    public async Task<ActionResult<GetEventPhotosDto>> Add(Guid eventId, [FromForm] PostEventPhotosDto dto)
    {
        return await _photos.AddAsync(User.GetUserId(), eventId, dto);
    }

    [HttpDelete("{photoId:guid}")]
    public async Task<IActionResult> Delete(Guid eventId, Guid photoId)
    {
        await _photos.DeleteAsync(User.GetUserId(), eventId, photoId);
        return NoContent();
    }
}
