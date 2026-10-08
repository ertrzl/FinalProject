using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IConfiguration _configuration;

    public SessionIssuer(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokens,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _refreshTokens = refreshTokens;
        _configuration = configuration;
    }

    public async Task<TokenResponseDto> IssueAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email!, user.FullName, user.SecurityStamp!, roles);

        var now = DateTime.UtcNow;
        var refreshTokenDays = int.Parse(_configuration["Jwt:RefreshTokenExpiryDays"] ?? "30");
        var refreshToken = _tokenService.GenerateRefreshToken();

        await _refreshTokens.DeleteExpiredAsync(user.Id, now);
        await _refreshTokens.AddAsync(new DomainRefreshToken
        {
            UserId = user.Id,
            TokenHash = Hash(refreshToken),
            ExpiresAt = now.AddDays(refreshTokenDays)
        });
        await _refreshTokens.SaveChangesAsync();

        return new TokenResponseDto
        {
            AccessToken = accessToken.AccessToken,
            ExpiresAt = accessToken.ExpiresAt,
            RefreshToken = refreshToken,
            UserId = user.Id,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl
        };
    }

    // Rotation: a used refresh token is dead even if it hadn't expired yet, so a leaked one has a single use.
    public async Task<TokenResponseDto> RotateAsync(string refreshToken)
    {
        var now = DateTime.UtcNow;
        var stored = await FindAsync(refreshToken);

        if (stored == null || stored.ExpiresAt < now)
            throw new UnauthorizedException("Invalid or expired refresh token.");

        if (stored.RevokedAt != null)
        {
            if (now - stored.RevokedAt.Value > ReuseGracePeriod)
                await _refreshTokens.RevokeAllAsync(stored.UserId, now);

            throw new UnauthorizedException("Invalid or expired refresh token.");
        }

        var user = await _userManager.FindByIdAsync(stored.UserId.ToString())
            ?? throw new UnauthorizedException("Invalid or expired refresh token.");

        // Two requests with the same token at once: only the one that flips RevokedAt first gets a new pair.
        if (!await _refreshTokens.TryRevokeAsync(stored.Id, now))
            throw new UnauthorizedException("Invalid or expired refresh token.");

        return await IssueAsync(user);
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
