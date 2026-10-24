using Microsoft.Extensions.Logging;
using SocialNetworkPlatformProject.Application.Interfaces.Services;

namespace SocialNetworkPlatformProject.Infrastructure.Email;

public class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger)
    {
        _logger = logger;
    }

    public bool CanDeliver => false;

    public Task SendAsync(EmailMessage message)
    {
        Console.WriteLine($"""

            E-mail NOT sent: no SMTP account is set up (Email:Smtp:Host) and this is Development, so it is shown here instead.
            To: {message.To}
            Subject: {message.Subject}

            {message.TextBody}

            """);

        _logger.LogInformation("E-mail to {To} was shown on the console instead of being sent (no SMTP account, Development). Subject: {Subject}",
            message.To, message.Subject);

        return Task.CompletedTask;
    }
}
