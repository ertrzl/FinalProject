using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SocialNetworkPlatformProject.Application.Exceptions;

namespace SocialNetworkPlatformProject.Persistence.Identity;

// The one place a typed-in password is checked against an account, so the lockout rule (Identity options:
// a few wrong guesses and the account is locked for a while) applies to every route that asks for the password:
// login, "change password" and "delete account". Without it the second and third would be free guessing oracles.
public class PasswordVerifier
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<PasswordVerifier> _logger;

    public PasswordVerifier(UserManager<ApplicationUser> userManager, ILogger<PasswordVerifier> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    // True when the password is right. False when it is wrong. Throws a 429 while the account is locked
    // (also on the guess that triggers the lock, so a client never gets a "try again" it cannot use).
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
