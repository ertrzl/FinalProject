using SocialNetworkPlatformProject.Application.Common;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// Sends an e-mail. Implemented in Infrastructure: through an SMTP account when one is configured ("Email:Smtp"),
// otherwise it is written to the console (Development only) or dropped with a warning.
public interface IEmailSender
{
    Task SendAsync(EmailMessage message);
}
