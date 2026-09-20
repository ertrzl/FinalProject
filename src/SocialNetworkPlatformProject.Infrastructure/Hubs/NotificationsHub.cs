using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SocialNetworkPlatformProject.Infrastructure.Hubs;

// Server -> client only: INotificationService pushes "ReceiveNotification" through SignalRNotifier.
[Authorize]
public class NotificationsHub : Hub
{
}
