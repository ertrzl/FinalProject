using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SocialNetworkPlatformProject.Application.DTOs.Users;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Persistence.Identity;
using DomainRefreshToken = SocialNetworkPlatformProject.Domain.Entities.RefreshToken;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IFileStorageService _files;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IFileStorageService files,
        IRefreshTokenRepository refreshTokens,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _files = files;
        _refreshTokens = refreshTokens;
        _configuration = configuration;
    }

    public async Task<TokenResponseDto> RegisterAsync(RegisterDto dto)
    {
        if (await _userManager.FindByEmailAsync(dto.Email) != null)
            throw new ConflictException("This email is already registered.");

        if (await _userManager.FindByNameAsync(dto.UserName) != null)
            throw new ConflictException("This username is already taken.");

        string? avatarUrl = null;
        if (dto.Avatar != null)
            avatarUrl = await _files.SaveImageAsync(dto.Avatar, "avatars");

        var user = new ApplicationUser
        {
            UserName = dto.UserName.Trim(),
            Email = dto.Email.Trim(),
            FullName = dto.FullName.Trim(),
            BirthDate = dto.BirthDate,
            AvatarUrl = avatarUrl,
            LastSeenAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            _files.Delete(avatarUrl);
            throw new BadRequestException(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        return await BuildTokenResponseAsync(user);
    }

    public async Task<TokenResponseDto> LoginAsync(LoginDto dto)
    {
        var identifier = dto.EmailOrUserName.Trim();

        var user = identifier.Contains('@')
            ? await _userManager.FindByEmailAsync(identifier)
            : await _userManager.FindByNameAsync(identifier);

        if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            throw new UnauthorizedException("Invalid email/username or password.");

        user.LastSeenAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return await BuildTokenResponseAsync(user);
    }

    public async Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenDto dto)
    {
        var stored = await _refreshTokens.GetAll(t => t.Token == dto.RefreshToken).FirstOrDefaultAsync();
        if (stored == null || stored.IsRevoked || stored.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedException("Invalid or expired refresh token.");

        var user = await _userManager.FindByIdAsync(stored.UserId.ToString())
            ?? throw new UnauthorizedException("Invalid or expired refresh token.");

        // Rotation: a used refresh token is dead even if it hadn't expired yet, so a leaked one has a single use.
        stored.IsRevoked = true;
        _refreshTokens.Update(stored);
        await _refreshTokens.SaveChangesAsync();

        return await BuildTokenResponseAsync(user);
    }

    public async Task RevokeRefreshTokenAsync(RefreshTokenDto dto)
    {
        var stored = await _refreshTokens.GetAll(t => t.Token == dto.RefreshToken).FirstOrDefaultAsync();
        if (stored == null || stored.IsRevoked)
            return;

        stored.IsRevoked = true;
        _refreshTokens.Update(stored);
        await _refreshTokens.SaveChangesAsync();
    }

    private async Task<TokenResponseDto> BuildTokenResponseAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email!, user.FullName, roles);

        var refreshTokenDays = int.Parse(_configuration["Jwt:RefreshTokenExpiryDays"] ?? "30");
        var refreshToken = new DomainRefreshToken
        {
            UserId = user.Id,
            Token = _tokenService.GenerateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenDays)
        };
        await _refreshTokens.AddAsync(refreshToken);
        await _refreshTokens.SaveChangesAsync();

        return new TokenResponseDto
        {
            AccessToken = accessToken.AccessToken,
            ExpiresAt = accessToken.ExpiresAt,
            RefreshToken = refreshToken.Token,
            UserId = user.Id,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl
        };
    }
}
