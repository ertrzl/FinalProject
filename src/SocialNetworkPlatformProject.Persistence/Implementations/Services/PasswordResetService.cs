using System.Net;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Users;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Persistence.Identity;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class PasswordResetService : IPasswordResetService
{
    // The same text for a wrong, used, expired or somebody else's code, so the reply says nothing about which it was.
    private const string InvalidLinkMessage = "This password reset link is invalid or has expired. Request a new one.";

    // One reset mail per address per minute, so nobody can fill a stranger's inbox. Counted for unknown addresses too.
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromMinutes(1);

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SessionIssuer _sessions;
    private readonly IAccessTokenRevoker _accessTokens;
    private readonly IEmailSender _email;
    private readonly IMemoryCache _cache;
    private readonly IOptions<DataProtectionTokenProviderOptions> _tokenOptions;
    private readonly ILogger<PasswordResetService> _logger;

    public PasswordResetService(
        UserManager<ApplicationUser> userManager,
        SessionIssuer sessions,
        IAccessTokenRevoker accessTokens,
        IEmailSender email,
        IMemoryCache cache,
        IOptions<DataProtectionTokenProviderOptions> tokenOptions,
        ILogger<PasswordResetService> logger)
    {
        _userManager = userManager;
        _sessions = sessions;
        _accessTokens = accessTokens;
        _email = email;
        _cache = cache;
        _tokenOptions = tokenOptions;
        _logger = logger;
    }

    public async Task RequestAsync(ForgotPasswordDto dto, string? linkBaseUrl)
    {
        var email = dto.Email.Trim();

        var cooldownKey = "password-reset:" + email.ToLowerInvariant();
        if (_cache.TryGetValue(cooldownKey, out _))
            return;
        _cache.Set(cooldownKey, true, ResendCooldown);

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return;

        if (linkBaseUrl == null)
        {
            _logger.LogWarning("A password reset was requested but no link was made: set App:PublicBaseUrl to the site's public address.");
            return;
        }

        // Identity's own reset code: tied to the account's security stamp (so it dies when the password changes) and
        // valid for the time set in PasswordReset:LinkLifetimeMinutes.
        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
        var link = $"{linkBaseUrl}/reset-password.html?email={Uri.EscapeDataString(user.Email!)}&token={WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code))}";

        // Not awaited on purpose: sending takes seconds, and only an address that has an account would wait for
        // it. The reply would then show who is registered. Failures are logged by SendAsync.
        _ = SendAsync(BuildMessage(user, link));
    }

    public async Task ResetAsync(ResetPasswordDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email.Trim());
        var code = DecodeCode(dto.Token);
        if (user == null || code == null)
            throw new BadRequestException(InvalidLinkMessage);

        var oldStamp = user.SecurityStamp!; // Identity replaces it as part of the reset
        var result = await _userManager.ResetPasswordAsync(user, code, dto.NewPassword);
        if (!result.Succeeded)
        {
            // A concurrency failure means another request used the same code a moment earlier: it is spent now.
            if (result.Errors.Any(error => error.Code is nameof(IdentityErrorDescriber.InvalidToken) or nameof(IdentityErrorDescriber.ConcurrencyFailure)))
                throw new BadRequestException(InvalidLinkMessage);

            // A password the rules refuse (the code is still unused, so the person can simply try another one).
            throw new BadRequestException(string.Join(" ", result.Errors.Select(error => error.Description)));
        }

        // Whoever had the old password is signed out everywhere: refresh tokens revoked, access tokens (which carry
        // the old security stamp) refused from the next request on.
        _accessTokens.Revoke(user.Id, oldStamp);
        await _sessions.RevokeAllAsync(user.Id);

        // The person may have been locked out by too many wrong guesses before giving up and asking for this reset.
        user.LockoutEnd = null;
        user.AccessFailedCount = 0;
        await _userManager.UpdateAsync(user);
    }

    private static string? DecodeCode(string token)
    {
        try
        {
            return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private EmailMessage BuildMessage(ApplicationUser user, string link)
    {
        var lifetime = Describe(_tokenOptions.Value.TokenLifespan);
        var htmlLink = WebUtility.HtmlEncode(link);
        var htmlName = WebUtility.HtmlEncode(user.FullName);

        var text = $"""
            Merhaba {user.FullName},

            SocialNet hesabın için şifre sıfırlama isteği aldık. Yeni şifre belirlemek için bu bağlantıyı aç:

            {link}

            Bağlantı {lifetime} geçerlidir ve yalnızca bir kez kullanılabilir.
            Bu isteği sen yapmadıysan bu e-postayı yok sayabilirsin, şifren değişmez.
            """;

        var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:480px;margin:auto;color:#1c2436;line-height:1.5">
              <h2 style="color:#4a6cf7;margin-bottom:4px">SocialNet</h2>
              <p>Merhaba {htmlName},</p>
              <p>Hesabın için şifre sıfırlama isteği aldık. Yeni şifre belirlemek için aşağıdaki düğmeye tıkla.</p>
              <p><a href="{htmlLink}" style="display:inline-block;background:#4a6cf7;color:#ffffff;padding:10px 24px;border-radius:24px;text-decoration:none;font-weight:bold">Şifremi sıfırla</a></p>
              <p style="font-size:13px;color:#6b7385">Bağlantı {lifetime} geçerlidir ve yalnızca bir kez kullanılabilir. Düğme çalışmazsa bu adresi tarayıcına yapıştır:<br>{htmlLink}</p>
              <p style="font-size:13px;color:#6b7385">Bu isteği sen yapmadıysan bu e-postayı yok sayabilirsin, şifren değişmez.</p>
            </div>
            """;

        return new EmailMessage(user.Email!, "SocialNet şifre sıfırlama", text, html);
    }

    private static string Describe(TimeSpan lifespan) => lifespan.TotalMinutes < 60
        ? $"{Math.Max(1, (int)Math.Round(lifespan.TotalMinutes))} dakika"
        : $"{(int)Math.Round(lifespan.TotalHours)} saat";

    private async Task SendAsync(EmailMessage message)
    {
        try
        {
            await _email.SendAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "The password reset e-mail could not be sent.");
        }
    }
}
