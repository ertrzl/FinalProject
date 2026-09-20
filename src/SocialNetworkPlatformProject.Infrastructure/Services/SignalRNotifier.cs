using Microsoft.AspNetCore.SignalR;
using SocialNetworkPlatformProject.Application.DTOs.Messages;
using SocialNetworkPlatformProject.Application.DTOs.Notifications;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Infrastructure.Hubs;

namespace SocialNetworkPlatformProject.Infrastructure.Services;

public class SignalRNotifier : IRealTimeNotifier
{
    private readonly IHubContext<NotificationsHub> _notificationsHub;
    private readonly IHubContext<MessagesHub> _messagesHub;

    public SignalRNotifier(IHubContext<NotificationsHub> notificationsHub, IHubContext<MessagesHub> messagesHub)
    {
        _notificationsHub = notificationsHub;
        _messagesHub = messagesHub;
    }

    // Clients.User(...) reaches every open tab/device of that user (see SubClaimUserIdProvider).
    public Task SendNotificationAsync(Guid recipientId, GetNotificationDto notification)
    {
        return _notificationsHub.Clients.User(recipientId.ToString()).SendAsync("ReceiveNotification", notification);
    }

    public Task SendMessageAsync(Guid recipientId, GetMessageDto message)
    {
        return _messagesHub.Clients.User(recipientId.ToString()).SendAsync("ReceiveMessage", message);
    }
}
