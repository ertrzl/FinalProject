using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Application.Common;

// What a user chose to be notified about (settings.html "Bildirim Tercihleri"). Only the three kinds below can be
// switched off; everything else (messages, groups, events, the marketplace...) always notifies, because missing those
// would mean missing something the user has to act on.
public record NotificationPreferences(bool Likes, bool Comments, bool FriendRequests)
{
    // Should a notification of this type be created for the user?
    public bool Allows(NotificationType type) => type switch
    {
        NotificationType.PostLiked or NotificationType.CommentLiked => Likes,
        NotificationType.CommentAdded or NotificationType.CommentReplied or NotificationType.EventCommentAdded => Comments,
        NotificationType.FriendRequestReceived or NotificationType.FriendRequestAccepted => FriendRequests,
        _ => true
    };
}
