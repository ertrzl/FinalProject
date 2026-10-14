using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialNetworkPlatformProject.Application.DTOs.Admin;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Extensions;

namespace SocialNetworkPlatformProject.Controllers;

// Any signed-in person can report a post, comment, profile or listing; the admins read the reports in the admin panel.
[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports)
    {
        _reports = reports;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PostReportDto dto)
    {
        await _reports.CreateAsync(User.GetUserId(), dto);
        return NoContent();
    }
}
