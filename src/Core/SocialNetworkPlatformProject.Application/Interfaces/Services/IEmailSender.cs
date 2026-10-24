namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message);
}
