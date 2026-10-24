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

public class SessionIssuer
{
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

    public async Task<TokenResponseDto> IssueAsync(ApplicationUser user, Guid? sessionId = null, DateTime? sessionStartedAt = null)
    {
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
