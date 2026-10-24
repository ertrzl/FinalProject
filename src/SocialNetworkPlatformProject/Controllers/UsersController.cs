using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialNetworkPlatformProject.Application.DTOs.Highlights;
using SocialNetworkPlatformProject.Application.DTOs.Posts;
using SocialNetworkPlatformProject.Application.DTOs.Users;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Extensions;

namespace SocialNetworkPlatformProject.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private static readonly Regex UserNamePattern = new("^[A-Za-z0-9._]{1,50}$", RegexOptions.Compiled);

    private readonly IUserService _users;
    private readonly IBlockService _blocks;
    private readonly IStoryHighlightService _highlights;
    private readonly IMutualConnectionService _mutual;
    private readonly IPostMentionService _taggedPosts;
    private readonly IWebHostEnvironment _environment;

    public UsersController(
        IUserService users,
        IBlockService blocks,
        IStoryHighlightService highlights,
        IMutualConnectionService mutual,
        IPostMentionService taggedPosts,
        IWebHostEnvironment environment)
    {
        _users = users;
        _blocks = blocks;
        _highlights = highlights;
        _mutual = mutual;
        _taggedPosts = taggedPosts;
        _environment = environment;
    }

    [AllowAnonymous]
    [HttpGet("/u/{userName}")]
    public async Task<IActionResult> ShortAddress(string userName)
    {
        var stored = UserNamePattern.IsMatch(userName) ? await _users.FindUserNameAsync(userName) : null;
        if (stored == null)
            return await NotFoundPageAsync();

        return LocalRedirect("/profile?u=" + Uri.EscapeDataString(stored));
    }

    private async Task<IActionResult> NotFoundPageAsync()
    {
        var page = _environment.WebRootFileProvider.GetFileInfo("404.html");
        if (!page.Exists || page.PhysicalPath == null)
            return NotFound();

        Response.Headers.CacheControl = "no-cache";
        return new ContentResult
        {
            StatusCode = StatusCodes.Status404NotFound,
            ContentType = "text/html; charset=utf-8",
            Content = await System.IO.File.ReadAllTextAsync(page.PhysicalPath)
        };
    }

    [HttpGet("blocked")]
    public async Task<ActionResult<List<GetBlockedUserDto>>> GetBlocked()
    {
        return await _blocks.GetBlockedAsync(User.GetUserId());
    }

    [HttpPost("{id:guid}/block")]
    public async Task<IActionResult> Block(Guid id)
    {
        await _blocks.BlockAsync(User.GetUserId(), id);
        return NoContent();
    }

    [HttpDelete("{id:guid}/block")]
    public async Task<IActionResult> Unblock(Guid id)
    {
        await _blocks.UnblockAsync(User.GetUserId(), id);
        return NoContent();
    }

    [HttpGet("search")]
    public async Task<ActionResult<PagedResult<GetUserSearchResultDto>>> Search([FromQuery] string term, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        return await _users.SearchAsync(term, User.GetUserId(), page, pageSize);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetUserProfileDto>> GetProfile(Guid id)
    {
        return await _users.GetProfileAsync(id, User.GetUserId());
    }

    [HttpGet("by-username/{userName}")]
    public async Task<ActionResult<GetUserProfileDto>> GetProfileByUserName(string userName)
    {
        return await _users.GetProfileByUserNameAsync(userName, User.GetUserId());
    }

    [HttpGet("{id:guid}/highlights")]
    public async Task<ActionResult<List<GetHighlightDto>>> GetHighlights(Guid id)
    {
        return await _highlights.GetForUserAsync(User.GetUserId(), id);
    }

    [HttpGet("{id:guid}/mutual")]
    public async Task<ActionResult<GetMutualConnectionsDto>> GetMutual(Guid id)
    {
        return await _mutual.GetAsync(User.GetUserId(), id);
    }

    [HttpGet("{id:guid}/tagged-posts")]
    public async Task<ActionResult<PagedResult<GetPostDto>>> GetTaggedPosts(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        return await _taggedPosts.GetTaggedPostsAsync(id, User.GetUserId(), page, pageSize);
    }

    [HttpPut("me/profile")]
    [RequestSizeLimit(UploadLimits.MaxRequestBytes)]
    public async Task<ActionResult<GetUserProfileDto>> UpdateProfile([FromForm] PutUserProfileDto dto)
    {
        return await _users.UpdateProfileAsync(User.GetUserId(), dto);
    }

    [HttpPut("me/privacy")]
    public async Task<IActionResult> UpdatePrivacy(PutPrivacySettingsDto dto)
    {
        await _users.UpdatePrivacyAsync(User.GetUserId(), dto);
        return NoContent();
    }

    [HttpGet("me/notification-settings")]
    public async Task<ActionResult<GetNotificationSettingsDto>> GetNotificationSettings()
    {
        return await _users.GetNotificationSettingsAsync(User.GetUserId());
    }

    [HttpPut("me/notification-settings")]
    public async Task<ActionResult<GetNotificationSettingsDto>> UpdateNotificationSettings(PutNotificationSettingsDto dto)
    {
        return await _users.UpdateNotificationSettingsAsync(User.GetUserId(), dto);
    }

    [HttpPut("me/password")]
    public async Task<ActionResult<TokenResponseDto>> ChangePassword(PutPasswordDto dto)
    {
        return await _users.ChangePasswordAsync(User.GetUserId(), dto);
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DeleteAccount(DeleteAccountDto dto)
    {
        await _users.DeleteAccountAsync(User.GetUserId(), dto);
        return NoContent();
    }
}
