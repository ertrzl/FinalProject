using Microsoft.Extensions.Logging;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.Interfaces.Services;

namespace SocialNetworkPlatformProject.Infrastructure.Email;

// Used outside Development when no SMTP account is set up. The message is dropped and only a warning is written: its
// body holds a password reset link, and a link in a log file would let anyone who can read the log take over accounts.
public class DisabledEmailSender : IEmailSender
{
    private readonly ILogger<DisabledEmailSender> _logger;

    public DisabledEmailSender(ILogger<DisabledEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(EmailMessage message)
    {
        _logger.LogWarning("An e-mail was not sent because no SMTP account is configured (Email:Smtp:Host). Subject: {Subject}", message.Subject);
        return Task.CompletedTask;
    }
}
