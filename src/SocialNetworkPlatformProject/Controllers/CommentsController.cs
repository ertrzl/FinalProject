using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialNetworkPlatformProject.Application.DTOs.Comments;
using SocialNetworkPlatformProject.Application.DTOs.Common;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Extensions;

namespace SocialNetworkPlatformProject.Controllers;

// F5: threaded comments under a post (home.html / profile.html)
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _comments;

    public CommentsController(ICommentService comments)
    {
        _comments = comments;
    }

    [HttpGet("by-post/{postId:guid}")]
    public async Task<ActionResult<List<GetCommentDto>>> GetByPost(Guid postId)
    {
        return await _comments.GetByPostAsync(User.GetUserId(), postId);
    }

    [HttpPost]
    public async Task<ActionResult<GetCommentDto>> Add(PostCommentDto dto)
    {
        return await _comments.AddAsync(User.GetUserId(), dto);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _comments.DeleteAsync(User.GetUserId(), id);
        return NoContent();
    }

    [HttpPost("{id:guid}/like")]
    public async Task<ActionResult<GetLikeResultDto>> ToggleLike(Guid id)
    {
        return await _comments.ToggleLikeAsync(User.GetUserId(), id);
    }
}
