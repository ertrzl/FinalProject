using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialNetworkPlatformProject.Application.DTOs.Groups;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Extensions;

namespace SocialNetworkPlatformProject.Controllers;

// groups.html: Gruplarım / Keşfet tabs
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupsController : ControllerBase
{
    private readonly IGroupService _groups;

    public GroupsController(IGroupService groups)
    {
        _groups = groups;
    }

    [HttpGet("mine")]
    public async Task<ActionResult<List<GetGroupDto>>> GetMyGroups()
    {
        return await _groups.GetMyGroupsAsync(User.GetUserId());
    }

    [HttpGet("discover")]
    public async Task<ActionResult<List<GetGroupDto>>> Discover([FromQuery] string? search)
    {
        return await _groups.DiscoverAsync(User.GetUserId(), search);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetGroupDto>> GetById(Guid id)
    {
        return await _groups.GetByIdAsync(User.GetUserId(), id);
    }

    [HttpPost]
    public async Task<ActionResult<GetGroupDto>> Create([FromForm] PostGroupDto dto)
    {
        var group = await _groups.CreateAsync(User.GetUserId(), dto);
        return CreatedAtAction(nameof(GetById), new { id = group.Id }, group);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<GetGroupDto>> Update(Guid id, [FromForm] PutGroupDto dto)
    {
        return await _groups.UpdateAsync(User.GetUserId(), id, dto);
    }

    [HttpPost("{id:guid}/join")]
    public async Task<ActionResult<GetGroupDto>> Join(Guid id)
    {
        return await _groups.JoinAsync(User.GetUserId(), id);
    }

    [HttpGet("{id:guid}/join-requests")]
    public async Task<ActionResult<List<GetJoinRequestDto>>> GetJoinRequests(Guid id)
    {
        return await _groups.GetJoinRequestsAsync(User.GetUserId(), id);
    }

    [HttpPost("{id:guid}/join-requests/{userId:guid}/approve")]
    public async Task<IActionResult> ApproveJoinRequest(Guid id, Guid userId)
    {
        await _groups.ApproveJoinRequestAsync(User.GetUserId(), id, userId);
        return NoContent();
    }

    [HttpPost("{id:guid}/join-requests/{userId:guid}/reject")]
    public async Task<IActionResult> RejectJoinRequest(Guid id, Guid userId)
    {
        await _groups.RejectJoinRequestAsync(User.GetUserId(), id, userId);
        return NoContent();
    }

    [HttpPost("{id:guid}/leave")]
    public async Task<IActionResult> Leave(Guid id)
    {
        await _groups.LeaveAsync(User.GetUserId(), id);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _groups.DeleteAsync(User.GetUserId(), id);
        return NoContent();
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
    {
        await _groups.RemoveMemberAsync(User.GetUserId(), id, userId);
        return NoContent();
    }

    [HttpPut("{id:guid}/members/{userId:guid}/role")]
    public async Task<ActionResult<GetGroupDto>> SetMemberRole(Guid id, Guid userId, [FromBody] SetGroupMemberRoleDto dto)
    {
        return await _groups.SetMemberRoleAsync(User.GetUserId(), id, userId, dto.Role);
    }

    [HttpPost("{id:guid}/transfer-ownership/{userId:guid}")]
    public async Task<ActionResult<GetGroupDto>> TransferOwnership(Guid id, Guid userId)
    {
        return await _groups.TransferOwnershipAsync(User.GetUserId(), id, userId);
    }

    [HttpPost("{id:guid}/invites/{userId:guid}")]
    public async Task<IActionResult> InviteMember(Guid id, Guid userId)
    {
        await _groups.InviteMemberAsync(User.GetUserId(), id, userId);
        return NoContent();
    }

    [HttpGet("invites/mine")]
    public async Task<ActionResult<List<GetGroupInviteDto>>> GetMyInvites()
    {
        return await _groups.GetMyInvitesAsync(User.GetUserId());
    }

    [HttpPost("{id:guid}/invites/accept")]
    public async Task<IActionResult> AcceptInvite(Guid id)
    {
        await _groups.AcceptInviteAsync(User.GetUserId(), id);
        return NoContent();
    }

    [HttpPost("{id:guid}/invites/decline")]
    public async Task<IActionResult> DeclineInvite(Guid id)
    {
        await _groups.DeclineInviteAsync(User.GetUserId(), id);
        return NoContent();
    }
}
