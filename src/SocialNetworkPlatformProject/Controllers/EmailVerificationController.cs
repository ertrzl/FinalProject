using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SocialNetworkPlatformProject.Application.DTOs.Users;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Extensions;

namespace SocialNetworkPlatformProject.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class EmailVerificationController : ControllerBase
{
    private readonly IEmailVerificationService _verification;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public EmailVerificationController(IEmailVerificationService verification, IConfiguration configuration, IHostEnvironment environment)
    {
        _verification = verification;
        _configuration = configuration;
        _environment = environment;
    }

    [HttpPost("verify-email")]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    public async Task<IActionResult> Verify(VerifyEmailDto dto)
    {
        await _verification.VerifyAsync(dto);
        return NoContent();
    }

    [HttpPost("resend-verification")]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    public async Task<IActionResult> Resend(ResendVerificationDto dto)
    {
        await _verification.ResendAsync(dto, Request.LinkBaseUrl(_configuration, _environment));
        return NoContent();
    }
}
