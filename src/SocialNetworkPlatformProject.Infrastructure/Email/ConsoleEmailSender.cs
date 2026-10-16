using Microsoft.Extensions.Logging;
using SocialNetworkPlatformProject.Application.Interfaces.Services;

namespace SocialNetworkPlatformProject.Infrastructure.Email;

// Used in Development when no SMTP account is set up: the e-mail is written to the console instead, link included, so
// the "forgot password" flow can be tried (and shown) without any mail account.
//
// The message goes straight to the console and NOT through the logger: the logger also writes the daily log file, and a
// password-reset link in a file that stays on disk for two weeks is a way into the account for anyone who reads it.
// The log only gets a line saying that a message was shown.
public class ConsoleEmailSender : IEmailSender
{
    private readonly ILogger<ConsoleEmailSender> _logger;

    public ConsoleEmailSender(ILogger<ConsoleEmailSender> logger)
    {
        _logger = logger;
    }

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
