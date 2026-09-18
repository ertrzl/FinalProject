namespace SocialNetworkPlatformProject.Application.DTOs.Notifications;

// notifications.html feed + real-time push via NotificationsHub (SignalR)
public class GetNotificationDto
{
    public Guid Id { get; set; }

    public Guid ActorId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string? ActorAvatarUrl { get; set; }

    public string Type { get; set; } = string.Empty; // FriendRequestReceived / PostLiked / CommentAdded / ...
    public Guid? PostId { get; set; }
    public Guid? CommentId { get; set; }
    public Guid? FriendRequestId { get; set; }

    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
