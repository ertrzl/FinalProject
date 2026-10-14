using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Admin;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Enums;
using SocialNetworkPlatformProject.Extensions;

namespace SocialNetworkPlatformProject.Controllers;

// The admin panel's API. Only a signed-in person with the Admin role gets past the [Authorize] below (a normal user
// gets 403, an anonymous one 401); what an admin may do to whom is decided in AdminService / ReportService.
[ApiController]
[Route("api/admin")]
[Authorize(Roles = AppRoles.Admin)]
public class AdminController : ControllerBase
{
    private readonly IAdminService _admin;
    private readonly IReportService _reports;
    private readonly IAuditService _audit;

    public AdminController(IAdminService admin, IReportService reports, IAuditService audit)
    {
        _admin = admin;
        _reports = reports;
        _audit = audit;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<GetAdminStatsDto>> GetStats()
    {
        return await _admin.GetStatsAsync();
    }

    // ---- Reports ----

    [HttpGet("reports")]
    public async Task<ActionResult<PagedResult<GetReportDto>>> GetReports([FromQuery] ReportStatus? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        return await _reports.GetPageAsync(status, page, pageSize);
    }

    [HttpPost("reports/{id:guid}/resolve")]
    public async Task<ActionResult<GetReportDto>> ResolveReport(Guid id, [FromBody] ResolveReportDto dto)
    {
        return await _reports.ResolveAsync(User.GetUserId(), id, dto);
    }

    // ---- Users ----

    [HttpGet("users")]
    public async Task<ActionResult<PagedResult<GetAdminUserDto>>> GetUsers([FromQuery] string? term, [FromQuery] bool? suspended, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        return await _admin.SearchUsersAsync(term, suspended, page, pageSize);
    }

    [HttpPost("users/{id:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid id, [FromBody] SuspendUserDto dto)
    {
        await _admin.SuspendUserAsync(User.GetUserId(), id, dto.Reason);
        return NoContent();
    }

    [HttpPost("users/{id:guid}/unsuspend")]
    public async Task<IActionResult> Unsuspend(Guid id)
    {
        await _admin.UnsuspendUserAsync(User.GetUserId(), id);
        return NoContent();
    }

    [HttpDelete("users/{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        await _admin.DeleteUserAsync(User.GetUserId(), id);
        return NoContent();
    }

    // ---- Content ----

    [HttpGet("content/{type}")]
    public async Task<ActionResult<PagedResult<GetAdminContentDto>>> SearchContent(string type, [FromQuery] string? term, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        return await _admin.SearchContentAsync(type, term, page, pageSize);
    }

    [HttpDelete("content/{type}/{id:guid}")]
    public async Task<IActionResult> RemoveContent(string type, Guid id, [FromBody] RemoveContentDto? dto)
    {
        await _admin.RemoveContentAsync(User.GetUserId(), type, id, dto?.Note);
        return NoContent();
    }

    // ---- Log ----

    [HttpGet("audit")]
    public async Task<ActionResult<PagedResult<GetAuditLogDto>>> GetAudit([FromQuery] string? action, [FromQuery] string? term, [FromQuery] int page = 1, [FromQuery] int pageSize = 30)
    {
        return await _audit.GetPageAsync(action, term, page, pageSize);
    }
}
