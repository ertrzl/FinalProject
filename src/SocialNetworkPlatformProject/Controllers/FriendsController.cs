using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialNetworkPlatformProject.Application.DTOs.Friends;
using SocialNetworkPlatformProject.Application.DTOs.Users;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Extensions;

namespace SocialNetworkPlatformProject.Controllers;

// F2: friends.html tabs (Arkadaşlarım / Gelen İstekler / Gönderilen İstekler)
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FriendsController : ControllerBase
{
    private readonly IFriendService _friends;

    public FriendsController(IFriendService friends)
    {
        _friends = friends;
    }

    [HttpGet]
    public async Task<ActionResult<List<GetFriendDto>>> GetMyFriends()
    {
        return await _friends.GetFriendsAsync(User.GetUserId());
    }

    [HttpGet("requests/incoming")]
    public async Task<ActionResult<List<GetFriendRequestDto>>> GetIncomingRequests()
    {
        return await _friends.GetIncomingRequestsAsync(User.GetUserId());
    }

    [HttpGet("requests/sent")]
    public async Task<ActionResult<List<GetFriendRequestDto>>> GetSentRequests()
    {
        return await _friends.GetSentRequestsAsync(User.GetUserId());
    }

    [HttpPost("requests")]
    public async Task<ActionResult<GetFriendRequestDto>> SendRequest(PostFriendRequestDto dto)
    {
        return await _friends.SendRequestAsync(User.GetUserId(), dto);
    }

    [HttpPost("requests/{requestId:guid}/accept")]
    public async Task<IActionResult> AcceptRequest(Guid requestId)
    {
        await _friends.AcceptRequestAsync(User.GetUserId(), requestId);
        return NoContent();
    }

    [HttpPost("requests/{requestId:guid}/decline")]
    public async Task<IActionResult> DeclineRequest(Guid requestId)
    {
        await _friends.DeclineRequestAsync(User.GetUserId(), requestId);
        return NoContent();
    }

    [HttpPost("requests/{requestId:guid}/cancel")]
    public async Task<IActionResult> CancelRequest(Guid requestId)
    {
        await _friends.CancelRequestAsync(User.GetUserId(), requestId);
        return NoContent();
    }

    [HttpDelete("{friendUserId:guid}")]
    public async Task<IActionResult> RemoveFriend(Guid friendUserId)
    {
        await _friends.RemoveFriendAsync(User.GetUserId(), friendUserId);
        return NoContent();
    }

    [HttpGet("suggestions")]
    public async Task<ActionResult<List<GetUserSearchResultDto>>> GetSuggestions([FromQuery] int take = 10)
    {
        return await _friends.GetSuggestionsAsync(User.GetUserId(), take);
    }
}
