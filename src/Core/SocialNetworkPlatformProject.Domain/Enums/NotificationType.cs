namespace SocialNetworkPlatformProject.Domain.Enums;

// Matches notifications.html badge icons: person-plus (friend), heart (like), chat (comment), check (accepted)
public enum NotificationType
{
    FriendRequestReceived = 0,
    FriendRequestAccepted = 1,
    PostLiked = 2,
    CommentAdded = 3,
    CommentLiked = 4
}
