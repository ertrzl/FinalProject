using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SocialNetworkPlatformProject.Application.DTOs.Users;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using DomainRefreshToken = SocialNetworkPlatformProject.Domain.Entities.RefreshToken;

namespace SocialNetworkPlatformProject.Persistence.Identity;

// Everything about logged-in sessions: handing out an access token + refresh token pair, trading a refresh
// token for a new pair, and ending sessions. Only a hash of each refresh token is stored.
public class SessionIssuer
{
    // A refresh token used a second time within this window is almost certainly two tabs refreshing together
    // (the loser just retries with the new token). Later than that it means a copy of the token is out there.
    private static readonly TimeSpan ReuseGracePeriod = TimeSpan.FromSeconds(30);

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IConfiguration _configuration;
    private readonly IClientInfo _client;
    private readonly ILogger<SessionIssuer> _logger;

    public SessionIssuer(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokens,
        IConfiguration configuration,
        IClientInfo client,
        ILogger<SessionIssuer> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _refreshTokens = refreshTokens;
        _configuration = configuration;
        _client = client;
        _logger = logger;
    }

    // A sign-in starts a new session; a renewal (RotateAsync) passes the session it continues, so the device keeps its place
    // in the list of open sessions.
    public async Task<TokenResponseDto> IssueAsync(ApplicationUser user, Guid? sessionId = null, DateTime? sessionStartedAt = null)
    {
        // A suspended account gets no session, whatever way it asks (sign-in or a refresh token).
        if (user.IsSuspended)
        {
            _logger.LogWarning("Sign-in refused: {UserName} ({UserId:l}) is suspended", user.UserName, user.Id.ToString());
            throw new ForbiddenException(Loc.T("auth.suspended"));
        }

        await EnsureConfiguredAdminAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var session = sessionId ?? Guid.NewGuid();
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email!, user.FullName, user.SecurityStamp!, session, roles);
        var mediaToken = _tokenService.GenerateMediaToken(user.Id, user.SecurityStamp!, session);

        var now = DateTime.UtcNow;
        var refreshTokenDays = int.Parse(_configuration["Jwt:RefreshTokenExpiryDays"] ?? "30");
        var refreshToken = _tokenService.GenerateRefreshToken();

        await _refreshTokens.DeleteExpiredAsync(user.Id, now);
        await _refreshTokens.AddAsync(new DomainRefreshToken
        {
            UserId = user.Id,
            TokenHash = Hash(refreshToken),
            ExpiresAt = now.AddDays(refreshTokenDays),
            SessionId = session,
            SessionStartedAt = sessionStartedAt ?? now,
            DeviceName = DeviceNames.FromUserAgent(_client.UserAgent),
            IpAddress = _client.IpAddress
        });
        await _refreshTokens.SaveChangesAsync();

        return new TokenResponseDto
        {
            AccessToken = accessToken.AccessToken,
            ExpiresAt = accessToken.ExpiresAt,
            RefreshToken = refreshToken,
            UserId = user.Id,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            IsAdmin = roles.Contains(AppRoles.Admin),
            MediaToken = mediaToken.AccessToken,
            MediaExpiresAt = mediaToken.ExpiresAt
        };
    }

    // The people who are admins are listed in appsettings.json ("Admin": { "Emails": [...] }). Whoever signs in (or
    // renews a session) with one of those e-mail addresses gets the role: the first time it is created and given.
    private async Task EnsureConfiguredAdminAsync(ApplicationUser user)
    {
        var emails = _configuration.GetSection("Admin:Emails").Get<string[]>() ?? Array.Empty<string>();
        if (!emails.Any(e => string.Equals(e?.Trim(), user.Email, StringComparison.OrdinalIgnoreCase)))
            return;

        if (!await _roleManager.RoleExistsAsync(AppRoles.Admin))
            await _roleManager.CreateAsync(new IdentityRole<Guid>(AppRoles.Admin));

        if (!await _userManager.IsInRoleAsync(user, AppRoles.Admin))
            await _userManager.AddToRoleAsync(user, AppRoles.Admin);
    }

    // Rotation: a used refresh token is dead even if it hadn't expired yet, so a leaked one has a single use.
    public async Task<TokenResponseDto> RotateAsync(string refreshToken)
    {
        var now = DateTime.UtcNow;
        var stored = await FindAsync(refreshToken);

        if (stored == null || stored.ExpiresAt < now)
            throw new UnauthorizedException(Loc.T("auth.sessionInvalid"));

        if (stored.RevokedAt != null)
        {
            if (now - stored.RevokedAt.Value > ReuseGracePeriod)
            {
                _logger.LogWarning("A used refresh token came back for user {UserId:l}: every session of the user was ended", stored.UserId.ToString());
                await _refreshTokens.RevokeAllAsync(stored.UserId, now);
            }

            throw new UnauthorizedException(Loc.T("auth.sessionInvalid"));
        }

        var user = await _userManager.FindByIdAsync(stored.UserId.ToString())
            ?? throw new UnauthorizedException(Loc.T("auth.sessionInvalid"));

        // Two requests with the same token at once: only the one that flips RevokedAt first gets a new pair.
        if (!await _refreshTokens.TryRevokeAsync(stored.Id, now))
            throw new UnauthorizedException(Loc.T("auth.sessionInvalid"));

        return await IssueAsync(user, stored.SessionId, stored.SessionStartedAt);
    }

    public async Task RevokeAsync(string refreshToken)
    {
        var stored = await FindAsync(refreshToken);
        if (stored != null)
            await _refreshTokens.TryRevokeAsync(stored.Id, DateTime.UtcNow);
    }

    // Ends every session of the user (all devices).
    public async Task RevokeAllAsync(Guid userId)
    {
        await _refreshTokens.RevokeAllAsync(userId, DateTime.UtcNow);
    }

    private async Task<DomainRefreshToken?> FindAsync(string refreshToken)
    {
        var hash = Hash(refreshToken);
        return await _refreshTokens.GetAll(t => t.TokenHash == hash, asNoTracking: true).FirstOrDefaultAsync();
    }

    private static string Hash(string refreshToken)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
    }
}
