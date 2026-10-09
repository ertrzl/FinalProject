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
    private readonly IPollService _polls;

    public PostsController(IPostService posts, IPollService polls)
    {
        _posts = posts;
        _polls = polls;
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

    [HttpGet("by-group/{groupId:guid}")]
    public async Task<ActionResult<PagedResult<GetPostDto>>> GetByGroup(Guid groupId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        return await _posts.GetGroupPostsAsync(groupId, User.GetUserId(), page, pageSize);
    }

    [HttpGet("search")]
    public async Task<ActionResult<PagedResult<GetPostDto>>> Search([FromQuery] string term, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        return await _posts.SearchAsync(User.GetUserId(), term, page, pageSize);
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
    public async Task<ActionResult<GetPostReactionResultDto>> ToggleLike(Guid id)
    {
        return await _posts.ToggleLikeAsync(User.GetUserId(), id);
    }

    [HttpPut("{id:guid}/reaction")]
    public async Task<ActionResult<GetPostReactionResultDto>> SetReaction(Guid id, PutReactionDto dto)
    {
        return await _posts.SetReactionAsync(User.GetUserId(), id, dto.Type);
    }

    [HttpGet("{id:guid}/reactions")]
    public async Task<ActionResult<List<GetPostReactorDto>>> GetReactions(Guid id, [FromQuery] string? type = null)
    {
        return await _posts.GetReactionsAsync(User.GetUserId(), id, type);
    }

    [HttpPost("{id:guid}/share")]
    public async Task<ActionResult<GetPostDto>> Share(Guid id, PostShareDto dto)
    {
        var share = await _posts.ShareAsync(User.GetUserId(), id, dto);
        return CreatedAtAction(nameof(GetById), new { id = share.Id }, share);
    }

    [HttpPost("{id:guid}/poll/vote")]
    public async Task<ActionResult<GetPostDto>> Vote(Guid id, PostPollVoteDto dto)
    {
        return await _polls.VoteAsync(User.GetUserId(), id, dto.OptionId);
    }

    [HttpPost("{id:guid}/save")]
    public async Task<ActionResult<bool>> ToggleSave(Guid id)
    {
        return await _posts.ToggleSaveAsync(User.GetUserId(), id);
    }
}
