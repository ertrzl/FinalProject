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
public class PasswordResetController : ControllerBase
{
    private readonly IPasswordResetService _passwordReset;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public PasswordResetController(IPasswordResetService passwordReset, IConfiguration configuration, IHostEnvironment environment)
    {
        _passwordReset = passwordReset;
        _configuration = configuration;
        _environment = environment;
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        await _passwordReset.RequestAsync(dto, Request.LinkBaseUrl(_configuration, _environment));
        return NoContent();
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        await _passwordReset.ResetAsync(dto);
        return NoContent();
    }
}
