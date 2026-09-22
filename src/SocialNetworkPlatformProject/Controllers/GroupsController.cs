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

    [HttpPost("{id:guid}/join")]
    public async Task<ActionResult<GetGroupDto>> Join(Guid id)
    {
        return await _groups.JoinAsync(User.GetUserId(), id);
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
}
