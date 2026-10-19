using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialNetworkPlatformProject.Application.DTOs.Highlights;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Extensions;

namespace SocialNetworkPlatformProject.Controllers;

// Story highlights: the owner's manage window and "Öne çıkanlara ekle" (profile.html, home.html story viewer) and the viewer that plays one.
// Somebody's covers on their profile are GET /api/users/{id}/highlights.
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HighlightsController : ControllerBase
{
    private readonly IStoryHighlightService _highlights;

    public HighlightsController(IStoryHighlightService highlights)
    {
        _highlights = highlights;
    }

    // Your own highlights; with a story id each one says whether that story is already in it.
    [HttpGet("mine")]
    public async Task<ActionResult<List<GetHighlightDto>>> GetMine([FromQuery] Guid? storyId = null)
    {
        var me = User.GetUserId();
        return await _highlights.GetForUserAsync(me, me, storyId);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetHighlightDetailDto>> Get(Guid id)
    {
        return await _highlights.GetAsync(User.GetUserId(), id);
    }

    [HttpPost]
    public async Task<ActionResult<GetHighlightDetailDto>> Create(PostHighlightDto dto)
    {
        return await _highlights.CreateAsync(User.GetUserId(), dto);
    }

    [HttpPost("{id:guid}/items")]
    public async Task<ActionResult<GetHighlightDetailDto>> AddStory(Guid id, PostHighlightItemDto dto)
    {
        return await _highlights.AddStoryAsync(User.GetUserId(), id, dto.StoryId);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<GetHighlightDetailDto>> Update(Guid id, PutHighlightDto dto)
    {
        return await _highlights.UpdateAsync(User.GetUserId(), id, dto);
    }

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid id, Guid itemId)
    {
        await _highlights.RemoveItemAsync(User.GetUserId(), id, itemId);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _highlights.DeleteAsync(User.GetUserId(), id);
        return NoContent();
    }
}
