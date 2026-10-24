using Microsoft.Extensions.Logging;
using SocialNetworkPlatformProject.Application.Interfaces.Services;

namespace SocialNetworkPlatformProject.Infrastructure.Email;

public class DisabledEmailSender : IEmailSender
{
    private readonly ILogger<DisabledEmailSender> _logger;

    public DisabledEmailSender(ILogger<DisabledEmailSender> logger)
    {
        _logger = logger;
    }

    public bool CanDeliver => false;

    public Task SendAsync(EmailMessage message)
    {
        _logger.LogWarning("An e-mail was not sent because no SMTP account is configured (Email:Smtp:Host). Subject: {Subject}", message.Subject);
        return Task.CompletedTask;
    }
}
