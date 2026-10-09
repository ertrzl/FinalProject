using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Posts;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Extensions;

namespace SocialNetworkPlatformProject.Controllers;

// explore.html (Keşfet): what is popular right now. People to meet come from GET /api/friends/suggestions.
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExploreController : ControllerBase
{
    private readonly IPostService _posts;

    public ExploreController(IPostService posts)
    {
        _posts = posts;
    }

    [HttpGet("trending-hashtags")]
    public async Task<ActionResult<List<GetTrendingHashtagDto>>> GetTrendingHashtags([FromQuery] int take = 10)
    {
        return await _posts.GetTrendingHashtagsAsync(User.GetUserId(), take);
    }

    [HttpGet("posts")]
    public async Task<ActionResult<PagedResult<GetPostDto>>> GetPopularPosts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        return await _posts.GetPopularPostsAsync(User.GetUserId(), page, pageSize);
    }
}
