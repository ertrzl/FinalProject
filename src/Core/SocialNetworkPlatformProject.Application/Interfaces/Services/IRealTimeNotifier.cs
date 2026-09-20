using SocialNetworkPlatformProject.Application.DTOs.Messages;
using SocialNetworkPlatformProject.Application.DTOs.Notifications;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// Implemented in Infrastructure with SignalR (NotificationsHub / MessagesHub).
public interface IRealTimeNotifier
{
    Task SendNotificationAsync(Guid recipientId, GetNotificationDto notification);

    Task SendMessageAsync(Guid recipientId, GetMessageDto message);
}
