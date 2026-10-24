using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SocialNetworkPlatformProject.Application.DTOs.Users;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Extensions;
using SocialNetworkPlatformProject.Infrastructure.Services;

namespace SocialNetworkPlatformProject.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IAccessTokenRevoker _revoker;
    private readonly ITokenService _tokens;
    private readonly ISessionService _sessions;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public AuthController(IAuthService auth, IAccessTokenRevoker revoker, ITokenService tokens, ISessionService sessions, IConfiguration configuration, IHostEnvironment environment)
    {
        _configuration = configuration;
        _environment = environment;
        _auth = auth;
        _revoker = revoker;
        _tokens = tokens;
        _sessions = sessions;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    [RequestSizeLimit(UploadLimits.MaxRequestBytes)]
    public async Task<ActionResult<RegisterResultDto>> Register([FromForm] RegisterDto dto)
    {
        return await _auth.RegisterAsync(dto, Request.LinkBaseUrl(_configuration, _environment));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    public async Task<ActionResult<TokenResponseDto>> Login(LoginDto dto)
    {
        return await _auth.LoginAsync(dto);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    public async Task<ActionResult<TokenResponseDto>> Refresh(RefreshTokenDto dto)
    {
        return await _auth.RefreshTokenAsync(dto);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(RefreshTokenDto dto)
    {
        await _auth.RevokeRefreshTokenAsync(dto);

        RevokeToken(User);
        var photoCookie = await HttpContext.AuthenticateAsync(MediaCookie.Scheme);
        if (photoCookie.Succeeded)
            RevokeToken(photoCookie.Principal);

        Response.Cookies.Delete(MediaCookie.Name, new CookieOptions { Path = MediaCookie.Path });
        return NoContent();
    }

    [HttpPost("media-session")]
    [Authorize]
    public ActionResult<MediaSessionDto> MediaSession()
    {
        var stamp = User.FindFirst(TokenService.SecurityStampClaim)?.Value;
        if (string.IsNullOrEmpty(stamp))
            return Unauthorized();

        var token = _tokens.GenerateMediaToken(User.GetUserId(), stamp, CurrentSessionId);
        HttpContext.AppendMediaCookie(token.AccessToken, token.ExpiresAt);
        return new MediaSessionDto { ExpiresAt = token.ExpiresAt };
    }


    [HttpGet("sessions")]
    [Authorize]
    public async Task<ActionResult<List<GetSessionDto>>> GetSessions()
    {
        return await _sessions.GetMineAsync(User.GetUserId(), CurrentSessionId);
    }

    [HttpDelete("sessions/{sessionId:guid}")]
    [Authorize]
    public async Task<IActionResult> EndSession(Guid sessionId)
    {
        await _sessions.EndAsync(User.GetUserId(), sessionId);
        return NoContent();
    }

    [HttpPost("sessions/end-others")]
    [Authorize]
    public async Task<ActionResult<int>> EndOtherSessions()
    {
        return await _sessions.EndOthersAsync(User.GetUserId(), CurrentSessionId);
    }

    private Guid? CurrentSessionId => Guid.TryParse(User.FindFirst(TokenService.SessionClaim)?.Value, out var id) ? id : null;

    private void RevokeToken(ClaimsPrincipal? principal)
    {
        var tokenId = principal?.FindFirst("jti")?.Value;
        if (tokenId == null || !long.TryParse(principal?.FindFirst("exp")?.Value, out var expiresAtSeconds))
            return;

        _revoker.RevokeToken(tokenId, DateTimeOffset.FromUnixTimeSeconds(expiresAtSeconds).UtcDateTime);
    }
}
