using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialNetworkPlatformProject.Application.DTOs.Messages;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Extensions;

namespace SocialNetworkPlatformProject.Controllers;

[ApiController]
[Route("api/messages/group-chats")]
[Authorize]
public class ChatGroupsController : ControllerBase
{
    private readonly IChatGroupService _chatGroups;

    public ChatGroupsController(IChatGroupService chatGroups)
    {
        _chatGroups = chatGroups;
    }

    [HttpPost]
    [RequestSizeLimit(UploadLimits.MaxRequestBytes)]
    public async Task<ActionResult<GetConversationDto>> Create([FromForm] PostChatGroupDto dto)
    {
        return await _chatGroups.CreateAsync(User.GetUserId(), dto);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetChatGroupDto>> Get(Guid id)
    {
        return await _chatGroups.GetAsync(User.GetUserId(), id);
    }

    [HttpPut("{id:guid}")]
    [RequestSizeLimit(UploadLimits.MaxRequestBytes)]
    public async Task<ActionResult<GetChatGroupDto>> Update(Guid id, [FromForm] PutChatGroupDto dto)
    {
        return await _chatGroups.UpdateAsync(User.GetUserId(), id, dto);
    }

    [HttpPost("{id:guid}/members")]
    public async Task<ActionResult<GetChatGroupDto>> AddMembers(Guid id, PostChatGroupMembersDto dto)
    {
        return await _chatGroups.AddMembersAsync(User.GetUserId(), id, dto);
    }

    [HttpDelete("{id:guid}/members/{memberId:guid}")]
    public async Task<ActionResult<GetChatGroupDto>> RemoveMember(Guid id, Guid memberId)
    {
        return await _chatGroups.RemoveMemberAsync(User.GetUserId(), id, memberId);
    }

    [HttpPut("{id:guid}/members/{memberId:guid}/admin")]
    public async Task<ActionResult<GetChatGroupDto>> SetAdmin(Guid id, Guid memberId, PutChatGroupAdminDto dto)
    {
        return await _chatGroups.SetAdminAsync(User.GetUserId(), id, memberId, dto.IsAdmin);
    }

    [HttpPost("{id:guid}/leave")]
    public async Task<IActionResult> Leave(Guid id)
    {
        await _chatGroups.LeaveAsync(User.GetUserId(), id);
        return NoContent();
    }

    [HttpPost("{id:guid}/mute")]
    public async Task<ActionResult<GetChatGroupDto>> Mute(Guid id)
    {
        return await _chatGroups.SetMutedAsync(User.GetUserId(), id, true);
    }

    [HttpPost("{id:guid}/unmute")]
    public async Task<ActionResult<GetChatGroupDto>> Unmute(Guid id)
    {
        return await _chatGroups.SetMutedAsync(User.GetUserId(), id, false);
    }
}
