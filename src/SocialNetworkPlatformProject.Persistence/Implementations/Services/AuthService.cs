using Microsoft.AspNetCore.Identity;
using SocialNetworkPlatformProject.Application.DTOs.Users;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Persistence.Identity;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IFileStorageService _files;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IFileStorageService files)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _files = files;
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
            Location = dto.Location?.Trim(),
            Bio = dto.Bio?.Trim(),
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

    private async Task<TokenResponseDto> BuildTokenResponseAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.GenerateAccessToken(user.Id, user.Email!, user.FullName, roles);

        return new TokenResponseDto
        {
            AccessToken = token.AccessToken,
            ExpiresAt = token.ExpiresAt,
            UserId = user.Id,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl
        };
    }
}
