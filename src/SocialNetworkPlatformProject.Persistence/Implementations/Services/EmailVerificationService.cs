using System.Net;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocialNetworkPlatformProject.Application.DTOs.Users;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Persistence.Identity;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class EmailVerificationService : IEmailVerificationService
{
    private static string InvalidLinkMessage => Loc.T("emailVerify.invalidLink");

    private static readonly TimeSpan ResendCooldown = TimeSpan.FromMinutes(1);

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _email;
    private readonly IMemoryCache _cache;
    private readonly IOptions<DataProtectionTokenProviderOptions> _tokenOptions;
    private readonly ILogger<EmailVerificationService> _logger;
    private readonly bool _enabled;

    public EmailVerificationService(
        UserManager<ApplicationUser> userManager,
        IEmailSender email,
        IMemoryCache cache,
        IOptions<DataProtectionTokenProviderOptions> tokenOptions,
        IConfiguration configuration,
        ILogger<EmailVerificationService> logger)
    {
        _userManager = userManager;
        _email = email;
        _cache = cache;
        _tokenOptions = tokenOptions;
        _logger = logger;
        _enabled = configuration.GetValue("Email:RequireVerification", true);
    }

    public bool IsRequired => _enabled && _email.CanDeliver;

    public async Task SendLinkAsync(Guid userId, string? linkBaseUrl)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || user.EmailConfirmed)
            return;

        await SendToAsync(user, linkBaseUrl);
    }

    public async Task ResendAsync(ResendVerificationDto dto, string? linkBaseUrl)
    {
        var identifier = dto.EmailOrUserName.Trim();

        var cooldownKey = "email-verification:" + identifier.ToLowerInvariant();
        if (_cache.TryGetValue(cooldownKey, out _))
            return;
        _cache.Set(cooldownKey, true, ResendCooldown);

        var user = identifier.Contains('@')
            ? await _userManager.FindByEmailAsync(identifier)
            : await _userManager.FindByNameAsync(identifier);
        if (user == null || user.EmailConfirmed)
            return;

        await SendToAsync(user, linkBaseUrl);
    }

    public async Task VerifyAsync(VerifyEmailDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email.Trim());
        var code = DecodeCode(dto.Token);
        if (user == null || code == null)
            throw new BadRequestException(InvalidLinkMessage);

        if (user.EmailConfirmed)
            return;

        var result = await _userManager.ConfirmEmailAsync(user, code);
        if (!result.Succeeded)
            throw new BadRequestException(InvalidLinkMessage);

        _logger.LogInformation("E-mail verified for {UserName} ({UserId:l})", user.UserName, user.Id.ToString());
    }

    private async Task SendToAsync(ApplicationUser user, string? linkBaseUrl)
    {
        if (linkBaseUrl == null)
        {
            _logger.LogWarning("A verification e-mail was not sent: set App:PublicBaseUrl to the site's public address.");
            return;
        }

        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var link = $"{linkBaseUrl}/verify-email?email={Uri.EscapeDataString(user.Email!)}&token={WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code))}";

        _ = DeliverAsync(BuildMessage(user, link));
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

        var hello = Loc.T("email.verify.hello", user.FullName);
        var helloHtml = Loc.T("email.verify.hello", htmlName);
        var validity = Loc.T("email.verify.validity", lifetime);
        var ignore = Loc.T("email.verify.ignore");

        var text = $"""
            {hello}

            {Loc.T("email.verify.introText")}

            {link}

            {validity}
            {ignore}
            """;

        var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:480px;margin:auto;color:#1c2436;line-height:1.5">
              <h2 style="color:#4a6cf7;margin-bottom:4px">Stoop</h2>
              <p>{helloHtml}</p>
              <p>{Loc.T("email.verify.introHtml")}</p>
              <p><a href="{htmlLink}" style="display:inline-block;background:#4a6cf7;color:#ffffff;padding:10px 24px;border-radius:24px;text-decoration:none;font-weight:bold">{Loc.T("email.verify.button")}</a></p>
              <p style="font-size:13px;color:#6b7385">{validity} {Loc.T("email.verify.buttonFallback")}<br>{htmlLink}</p>
              <p style="font-size:13px;color:#6b7385">{ignore}</p>
            </div>
            """;

        return new EmailMessage(user.Email!, Loc.T("email.verify.subject"), text, html);
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

    private async Task DeliverAsync(EmailMessage message)
    {
        try
        {
            await _email.SendAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "The verification e-mail could not be sent.");
        }
    }
}
