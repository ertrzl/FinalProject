using SocialNetworkPlatformProject.Application.DTOs.Messages;
using SocialNetworkPlatformProject.Application.DTOs.Notifications;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IRealTimeNotifier
{
    Task SendNotificationAsync(Guid recipientId, GetNotificationDto notification);

    Task SendMessageAsync(Guid recipientId, GetMessageDto message);

    Task PublishAsync(IEnumerable<Guid> recipientIds, string eventName, object payload);

    Task PublishToMessagesAsync(Guid recipientId, string eventName, object payload);
}
