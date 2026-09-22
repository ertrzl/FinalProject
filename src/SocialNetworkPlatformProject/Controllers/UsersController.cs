using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Users;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Extensions;

namespace SocialNetworkPlatformProject.Controllers;

// F6/F7: profile page + search + settings
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;

    public UsersController(IUserService users)
    {
        _users = users;
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

    [HttpPut("me/profile")]
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

    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword(PutPasswordDto dto)
    {
        await _users.ChangePasswordAsync(User.GetUserId(), dto);
        return NoContent();
    }

    [HttpDelete("me")]
    public async Task<IActionResult> DeleteAccount()
    {
        await _users.DeleteAccountAsync(User.GetUserId());
        return NoContent();
    }
}
