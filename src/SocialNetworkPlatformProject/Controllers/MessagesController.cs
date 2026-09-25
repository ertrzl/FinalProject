using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Messages;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Extensions;

namespace SocialNetworkPlatformProject.Controllers;

// messages.html + chat dock. Real-time delivery goes through MessagesHub (SignalR);
// these endpoints cover history and the non-JS fallback.
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messages;

    public MessagesController(IMessageService messages)
    {
        _messages = messages;
    }

    [HttpGet("conversations")]
    public async Task<ActionResult<List<GetConversationDto>>> GetConversations()
    {
        return await _messages.GetConversationsAsync(User.GetUserId());
    }

    [HttpGet("conversations/{conversationId:guid}")]
    public async Task<ActionResult<PagedResult<GetMessageDto>>> GetMessages(Guid conversationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 30)
    {
        return await _messages.GetMessagesAsync(User.GetUserId(), conversationId, page, pageSize);
    }

    [HttpPost("conversations/{conversationId:guid}/read")]
    public async Task<IActionResult> MarkAsRead(Guid conversationId)
    {
        await _messages.MarkConversationAsReadAsync(User.GetUserId(), conversationId);
        return NoContent();
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        return await _messages.GetUnreadCountAsync(User.GetUserId());
    }

    [HttpPost]
    public async Task<ActionResult<GetMessageDto>> Send(PostMessageDto dto)
    {
        return await _messages.SendAsync(User.GetUserId(), dto);
    }

    [HttpPost("image")]
    public async Task<ActionResult<GetMessageDto>> SendImage([FromForm] PostMessageImageDto dto)
    {
        return await _messages.SendImageAsync(User.GetUserId(), dto);
    }
}
