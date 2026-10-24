using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialNetworkPlatformProject.Application.DTOs.Messages;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Extensions;

namespace SocialNetworkPlatformProject.Controllers;

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

    [HttpPost("groups/{groupId:guid}")]
    public async Task<ActionResult<GetConversationDto>> OpenGroupChat(Guid groupId)
    {
        return await _messages.OpenGroupChatAsync(User.GetUserId(), groupId);
    }

    [HttpGet("conversations/{conversationId:guid}")]
    public async Task<ActionResult<PagedResult<GetMessageDto>>> GetMessages(Guid conversationId, [FromQuery] int page = 1, [FromQuery] int pageSize = 30)
    {
        return await _messages.GetMessagesAsync(User.GetUserId(), conversationId, page, pageSize);
    }

    [HttpDelete("{messageId:guid}")]
    public async Task<ActionResult<GetMessageDto>> Delete(Guid messageId)
    {
        return await _messages.DeleteAsync(User.GetUserId(), messageId);
    }

    [HttpPut("{messageId:guid}/reaction")]
    public async Task<ActionResult<GetMessageDto>> SetReaction(Guid messageId, PutMessageReactionDto dto)
    {
        return await _messages.SetReactionAsync(User.GetUserId(), messageId, dto.Emoji);
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
    [RequestSizeLimit(UploadLimits.MaxRequestBytes)]
    public async Task<ActionResult<GetMessageDto>> SendImage([FromForm] PostMessageImageDto dto)
    {
        return await _messages.SendImageAsync(User.GetUserId(), dto);
    }
}
