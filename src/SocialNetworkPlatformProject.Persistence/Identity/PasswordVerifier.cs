using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SocialNetworkPlatformProject.Application.Exceptions;

namespace SocialNetworkPlatformProject.Persistence.Identity;

public class PasswordVerifier
{
    private static readonly ApplicationUser Decoy = new();
    private static string? _decoyHash;

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<PasswordVerifier> _logger;

    public PasswordVerifier(UserManager<ApplicationUser> userManager, ILogger<PasswordVerifier> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public void SpendTimeLikeAWrongGuess(string password)
    {
        var hasher = _userManager.PasswordHasher;
        _decoyHash ??= hasher.HashPassword(Decoy, Guid.NewGuid().ToString("N"));
        hasher.VerifyHashedPassword(Decoy, _decoyHash, password);
    }

    public async Task<bool> VerifyAsync(ApplicationUser user, string password)
    {
        await EnsureNotLockedOutAsync(user);

        if (await _userManager.CheckPasswordAsync(user, password))
        {
            if (user.AccessFailedCount > 0)
                await _userManager.ResetAccessFailedCountAsync(user);

            return true;
        }

        await _userManager.AccessFailedAsync(user);
        await EnsureNotLockedOutAsync(user);
        return false;
    }

    private async Task EnsureNotLockedOutAsync(ApplicationUser user)
    {
        if (!await _userManager.IsLockedOutAsync(user))
            return;

        var remaining = (user.LockoutEnd ?? DateTimeOffset.UtcNow) - DateTimeOffset.UtcNow;
        var minutes = Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes));
        _logger.LogWarning("Account {UserName} ({UserId:l}) is locked out for {Minutes} more minute(s)", user.UserName, user.Id.ToString(), minutes);
        throw new TooManyRequestsException(Loc.T("auth.tooManyAttempts", minutes));
    }
}
