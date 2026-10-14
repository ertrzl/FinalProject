using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace SocialNetworkPlatformProject.Infrastructure.Hubs;

// Server -> client only: INotificationService pushes "ReceiveNotification" through SignalRNotifier.
[Authorize]
public class NotificationsHub : Hub
{
    private readonly ILogger<NotificationsHub> _logger;

    public NotificationsHub(ILogger<NotificationsHub> logger)
    {
        _logger = logger;
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogDebug("Notifications hub: user {UserId} connected ({ConnectionId})", Context.UserIdentifier, Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
            _logger.LogInformation(exception, "Notifications hub: connection of user {UserId} dropped ({ConnectionId})", Context.UserIdentifier, Context.ConnectionId);
        else
            _logger.LogDebug("Notifications hub: user {UserId} disconnected ({ConnectionId})", Context.UserIdentifier, Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
