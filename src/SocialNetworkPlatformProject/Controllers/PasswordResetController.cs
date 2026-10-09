using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SocialNetworkPlatformProject.Application.DTOs.Users;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Extensions;

namespace SocialNetworkPlatformProject.Controllers;

// index.html "Şifremi unuttum": forgot-password.html asks for the link, reset-password.html uses it
[ApiController]
[Route("api/auth")]
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
        await _passwordReset.RequestAsync(dto, LinkBaseUrl());
        return NoContent();
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        await _passwordReset.ResetAsync(dto);
        return NoContent();
    }

    // The link goes into an e-mail, so it is not built from the request's Host header: anyone can send a request with a
    // Host of their own, and the person asked for would get a link to the sender's site. The public address comes from
    // the settings ("App:PublicBaseUrl"); only in Development, which has none, the address the browser used will do.
    private string? LinkBaseUrl()
    {
        var configured = _configuration["App:PublicBaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.Trim().TrimEnd('/');

        return _environment.IsDevelopment() ? $"{Request.Scheme}://{Request.Host}" : null;
    }
}
