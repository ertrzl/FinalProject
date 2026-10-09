using Microsoft.Extensions.Logging;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.Interfaces.Services;

namespace SocialNetworkPlatformProject.Infrastructure.Email;

// Used in Development when no SMTP account is set up: the e-mail is written to the console instead, link included, so
// the "forgot password" flow can be tried (and shown) without any mail account.
public class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(EmailMessage message)
    {
        _logger.LogInformation("""
            E-mail NOT sent: no SMTP account is set up (Email:Smtp:Host) and this is Development, so it is shown here instead.
            To: {To}
            Subject: {Subject}

            {Body}
            """, message.To, message.Subject, message.TextBody);

        return Task.CompletedTask;
    }
}
