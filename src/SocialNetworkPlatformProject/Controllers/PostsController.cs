using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Common;
using SocialNetworkPlatformProject.Application.DTOs.Posts;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Extensions;

namespace SocialNetworkPlatformProject.Controllers;

// F3/F4/F5: composer, feed, likes, saved posts (home.html / profile.html / saved.html)
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PostsController : ControllerBase
{
    private readonly IPostService _posts;

    public PostsController(IPostService posts)
    {
        _posts = posts;
    }

    [HttpGet("feed")]
    public async Task<ActionResult<PagedResult<GetPostDto>>> GetFeed([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        return await _posts.GetFeedAsync(User.GetUserId(), page, pageSize);
    }

    [HttpGet("saved")]
    public async Task<ActionResult<PagedResult<GetPostDto>>> GetSaved([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        return await _posts.GetSavedPostsAsync(User.GetUserId(), page, pageSize);
    }

    [HttpGet("by-user/{userId:guid}")]
    public async Task<ActionResult<PagedResult<GetPostDto>>> GetByUser(Guid userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        return await _posts.GetUserPostsAsync(userId, User.GetUserId(), page, pageSize);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetPostDto>> GetById(Guid id)
    {
        return await _posts.GetByIdAsync(User.GetUserId(), id);
    }

    [HttpPost]
    public async Task<ActionResult<GetPostDto>> Create([FromForm] PostPostDto dto)
    {
        var post = await _posts.CreateAsync(User.GetUserId(), dto);
        return CreatedAtAction(nameof(GetById), new { id = post.Id }, post);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<GetPostDto>> Update(Guid id, PutPostDto dto)
    {
        return await _posts.UpdateAsync(User.GetUserId(), id, dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _posts.DeleteAsync(User.GetUserId(), id);
        return NoContent();
    }

    [HttpPost("{id:guid}/like")]
    public async Task<ActionResult<GetLikeResultDto>> ToggleLike(Guid id)
    {
        return await _posts.ToggleLikeAsync(User.GetUserId(), id);
    }

    [HttpPost("{id:guid}/save")]
    public async Task<ActionResult<bool>> ToggleSave(Guid id)
    {
        return await _posts.ToggleSaveAsync(User.GetUserId(), id);
    }
}
