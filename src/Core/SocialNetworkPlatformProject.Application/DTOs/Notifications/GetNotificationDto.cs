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
    public string? Reaction { get; set; } // PostLiked: which reaction (Love, Haha...); null on old "beğendi" notifications
    public Guid? FriendRequestId { get; set; }
    public Guid? GroupId { get; set; }
    public string? GroupName { get; set; }
    public Guid? OfferId { get; set; }
    public Guid? EventId { get; set; }
    public string? EventTitle { get; set; }
    public Guid? GameId { get; set; } // game invitation notifications: the game the button opens
    public DateTime? EventStartsAt { get; set; } // UTC; lets "event starts soon" show when
    public decimal? Amount { get; set; }
    public decimal? PreviousAmount { get; set; } // price-drop alerts: the price before the cut (Amount is the new one)
    public Guid? ConversationId { get; set; } // "added you to a chat" notifications: the chat the button opens
    public Guid? ListingId { get; set; }
    public string? ListingTitle { get; set; }

    // Friend request / group join request / group invitation notifications: is the request still waiting for an answer?
    // Only then the notification shows its Accept / Decline buttons; once answered (here or elsewhere) it is just text.
    public bool IsPending { get; set; }

    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
