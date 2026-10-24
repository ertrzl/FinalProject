namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IEmailSender
{
    bool CanDeliver { get; }

    Task SendAsync(EmailMessage message);
}
