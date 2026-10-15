using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SocialNetworkPlatformProject.Application.DTOs.Users;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Persistence.Identity;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly PasswordVerifier _passwords;
    private readonly SessionIssuer _sessions;
    private readonly IFileStorageService _files;
    private readonly LoginThrottle _unknownLogins;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        PasswordVerifier passwords,
        SessionIssuer sessions,
        IFileStorageService files,
        LoginThrottle unknownLogins,
        ILogger<AuthService> logger)
    {
        _unknownLogins = unknownLogins;
        _userManager = userManager;
        _passwords = passwords;
        _sessions = sessions;
        _files = files;
        _logger = logger;
    }

    public async Task<TokenResponseDto> RegisterAsync(RegisterDto dto)
    {
        var userName = dto.UserName.Trim();
        var email = dto.Email.Trim();

        if (await _userManager.FindByEmailAsync(email) != null)
            throw new ConflictException(Loc.T("auth.emailTaken"));

        if (await _userManager.FindByNameAsync(userName) != null)
            throw new ConflictException(Loc.T("auth.userNameTaken"));

        string? avatarUrl = null;
        if (dto.Avatar != null)
            avatarUrl = await _files.SaveImageAsync(dto.Avatar, "avatars");

        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
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

        // The name and the address of the new account may have been tried before it existed: they start clean.
        _unknownLogins.Forget(userName);
        _unknownLogins.Forget(email);

        var session = await _sessions.IssueAsync(user);
        _logger.LogInformation("New account: {UserName} ({UserId:l})", user.UserName, user.Id.ToString());
        return session;
    }

    public async Task<TokenResponseDto> LoginAsync(LoginDto dto)
    {
        var identifier = dto.EmailOrUserName.Trim();

        var user = identifier.Contains('@')
            ? await _userManager.FindByEmailAsync(identifier)
            : await _userManager.FindByNameAsync(identifier);

        // The name that was typed is not written: people sometimes type their password into that box.
        if (user == null)
        {
            // Answered like a real account is: locked after five wrong tries, and as slow as a password check (see LoginThrottle),
            // so neither the message nor the time tells whether the account exists.
            _unknownLogins.EnsureNotLocked(identifier);
            _passwords.SpendTimeLikeAWrongGuess(dto.Password);

            _logger.LogWarning("Failed sign-in: no such account");
            _unknownLogins.RecordFailure(identifier);
            throw new UnauthorizedException(Loc.T("auth.badCredentials"));
        }

        if (!await _passwords.VerifyAsync(user, dto.Password))
        {
            _logger.LogWarning("Failed sign-in: wrong password for {UserName} ({UserId:l}), attempt {Attempt}", user.UserName, user.Id.ToString(), user.AccessFailedCount);
            throw new UnauthorizedException(Loc.T("auth.badCredentials"));
        }

        user.LastSeenAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Asked after the session is made: a suspended account is refused there (and that is logged), it did not sign in.
        var session = await _sessions.IssueAsync(user);
        _logger.LogInformation("Signed in: {UserName} ({UserId:l})", user.UserName, user.Id.ToString());
        return session;
    }

    public async Task<TokenResponseDto> RefreshTokenAsync(RefreshTokenDto dto)
    {
        return await _sessions.RotateAsync(dto.RefreshToken);
    }

    public async Task RevokeRefreshTokenAsync(RefreshTokenDto dto)
    {
        await _sessions.RevokeAsync(dto.RefreshToken);
    }
}
