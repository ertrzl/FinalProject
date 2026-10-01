namespace SocialNetworkPlatformProject.Domain.Enums;

// Matches notifications.html badge icons: person-plus (friend), heart (like), chat (comment), check (accepted)
public enum NotificationType
{
    FriendRequestReceived = 0,
    FriendRequestAccepted = 1,
    PostLiked = 2,
    CommentAdded = 3,
    CommentLiked = 4,
    GroupJoinRequestReceived = 5, // sent to every group admin
    GroupInviteReceived = 6,
    GroupMemberRemoved = 7, // kicked
    GroupRoleChanged = 8,
    GroupJoinRequestApproved = 9,
    GroupJoinRequestRejected = 10
}
