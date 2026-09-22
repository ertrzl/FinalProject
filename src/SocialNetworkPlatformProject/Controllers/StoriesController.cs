using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialNetworkPlatformProject.Application.DTOs.Stories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Extensions;

namespace SocialNetworkPlatformProject.Controllers;

// home.html story bar + fullscreen viewer
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StoriesController : ControllerBase
{
    private readonly IStoryService _stories;

    public StoriesController(IStoryService stories)
    {
        _stories = stories;
    }

    [HttpGet]
    public async Task<ActionResult<List<GetStoryDto>>> GetActive()
    {
        return await _stories.GetActiveStoriesAsync(User.GetUserId());
    }

    [HttpPost]
    public async Task<ActionResult<GetStoryDto>> Create([FromForm] PostStoryDto dto)
    {
        return await _stories.CreateAsync(User.GetUserId(), dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _stories.DeleteAsync(User.GetUserId(), id);
        return NoContent();
    }
}
