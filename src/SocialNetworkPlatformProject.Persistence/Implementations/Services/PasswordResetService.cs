using System.Net;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialNetworkPlatformProject.Application.DTOs.Users;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Persistence.Identity;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class PasswordResetService : IPasswordResetService
{
    private static string InvalidLinkMessage => Loc.T("passwordReset.invalidLink");

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

        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
        _logger.LogInformation("Password reset link requested for {UserName} ({UserId:l})", user.UserName, user.Id.ToString());
        var link = $"{linkBaseUrl}/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code))}";

        _ = SendAsync(BuildMessage(user, link));
    }

    public async Task ResetAsync(ResetPasswordDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email.Trim());
        var code = DecodeCode(dto.Token);
        if (user == null || code == null)
            throw new BadRequestException(InvalidLinkMessage);

        var oldStamp = user.SecurityStamp!;
        var result = await _userManager.ResetPasswordAsync(user, code, dto.NewPassword);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(error => error.Code is nameof(IdentityErrorDescriber.InvalidToken) or nameof(IdentityErrorDescriber.ConcurrencyFailure)))
                throw new BadRequestException(InvalidLinkMessage);

            throw new BadRequestException(string.Join(" ", result.Errors.Select(error => error.Description)));
        }

        _accessTokens.Revoke(user.Id, oldStamp);
        await _sessions.RevokeAllAsync(user.Id);
        _logger.LogInformation("Password reset done for {UserName} ({UserId:l}); all sessions ended", user.UserName, user.Id.ToString());

        user.EmailConfirmed = true;
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

        var hello = Loc.T("email.reset.hello", user.FullName);
        var helloHtml = Loc.T("email.reset.hello", htmlName);
        var validity = Loc.T("email.reset.validity", lifetime);
        var ignore = Loc.T("email.reset.ignore");

        var text = $"""
            {hello}

            {Loc.T("email.reset.introText")}

            {link}

            {validity}
            {ignore}
            """;

        var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:480px;margin:auto;color:#1c2436;line-height:1.5">
              <h2 style="color:#4a6cf7;margin-bottom:4px">Stoop</h2>
              <p>{helloHtml}</p>
              <p>{Loc.T("email.reset.introHtml")}</p>
              <p><a href="{htmlLink}" style="display:inline-block;background:#4a6cf7;color:#ffffff;padding:10px 24px;border-radius:24px;text-decoration:none;font-weight:bold">{Loc.T("email.reset.button")}</a></p>
              <p style="font-size:13px;color:#6b7385">{validity} {Loc.T("email.reset.buttonFallback")}<br>{htmlLink}</p>
              <p style="font-size:13px;color:#6b7385">{ignore}</p>
            </div>
            """;

        return new EmailMessage(user.Email!, Loc.T("email.reset.subject"), text, html);
    }

    private static string Describe(TimeSpan lifespan)
    {
        if (lifespan.TotalMinutes < 60)
        {
            var minutes = Math.Max(1, (int)Math.Round(lifespan.TotalMinutes));
            return Loc.T(minutes == 1 ? "time.minute" : "time.minutes", minutes);
        }

        var hours = (int)Math.Round(lifespan.TotalHours);
        return Loc.T(hours == 1 ? "time.hour" : "time.hours", hours);
    }

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
