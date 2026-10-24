using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid RecipientId { get; set; }
    public Guid ActorId { get; set; }
    public NotificationType Type { get; set; }

    public Guid? PostId { get; set; }
    public Guid? CommentId { get; set; }
    public Guid? FriendRequestId { get; set; }
    public Guid? GroupId { get; set; }
    public Guid? OfferId { get; set; }
    public Guid? EventId { get; set; }
    public Guid? StoryId { get; set; }
    public Guid? GameId { get; set; }
    public Guid? ConversationId { get; set; }
    public Guid? ListingId { get; set; }
    public string? Subject { get; set; }
    public decimal? Amount { get; set; }
    public decimal? PreviousAmount { get; set; }

    public bool IsRead { get; set; } = false;
}
