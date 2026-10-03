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
    public Guid? GroupId { get; set; }
    public string? GroupName { get; set; }
    public Guid? OfferId { get; set; }
    public Guid? EventId { get; set; }
    public string? EventTitle { get; set; }
    public decimal? Amount { get; set; }
    public Guid? ListingId { get; set; }
    public string? ListingTitle { get; set; }

    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
