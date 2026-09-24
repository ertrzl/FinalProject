using SocialNetworkPlatformProject.Application.DTOs.Messages;
using SocialNetworkPlatformProject.Application.DTOs.Notifications;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// Implemented in Infrastructure with SignalR (NotificationsHub / MessagesHub).
public interface IRealTimeNotifier
{
    Task SendNotificationAsync(Guid recipientId, GetNotificationDto notification);

    Task SendMessageAsync(Guid recipientId, GetMessageDto message);

    // Named event to several users (feed updates), over the notifications connection.
    Task PublishAsync(IEnumerable<Guid> recipientIds, string eventName, object payload);

    // Named event to every connected client (presence), over the notifications connection.
    Task BroadcastAsync(string eventName, object payload);

    // Named event to one user over the messages connection (read receipts, typing).
    Task PublishToMessagesAsync(Guid recipientId, string eventName, object payload);
}
