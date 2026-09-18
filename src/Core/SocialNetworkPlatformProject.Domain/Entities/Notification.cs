using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

// F8: notifications.html feed + SignalR push (NotificationsHub)
public class Notification : BaseEntity
{
    public Guid RecipientId { get; set; }
    public Guid ActorId { get; set; }
    public NotificationType Type { get; set; }

    public Guid? PostId { get; set; }
    public Guid? CommentId { get; set; }
    public Guid? FriendRequestId { get; set; }

    public bool IsRead { get; set; } = false;
}
