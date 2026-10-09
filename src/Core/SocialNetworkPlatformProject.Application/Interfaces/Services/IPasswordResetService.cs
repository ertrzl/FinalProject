using SocialNetworkPlatformProject.Application.DTOs.Users;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IPasswordResetService
{
    // Mails a reset link to the address when an account has it. It completes the same way whether or not the address
    // is registered, so nobody can use it to find out who has an account. linkBaseUrl is the site's public address the
    // link starts with; without one no link is made (see PasswordResetController).
    Task RequestAsync(ForgotPasswordDto dto, string? linkBaseUrl);

    // Sets the new password when the code from the link is valid (it works once and expires), unlocks the account and
    // signs it out everywhere.
    Task ResetAsync(ResetPasswordDto dto);
}
