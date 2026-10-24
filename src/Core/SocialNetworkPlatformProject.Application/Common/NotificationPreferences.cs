using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Application.Common;

public record NotificationPreferences(bool Likes, bool Comments, bool FriendRequests, bool StoryTags)
{
    public bool Allows(NotificationType type) => type switch
    {
        NotificationType.PostLiked or NotificationType.CommentLiked or NotificationType.StoryLiked => Likes,
        NotificationType.CommentAdded or NotificationType.CommentReplied or NotificationType.EventCommentAdded or NotificationType.Mentioned => Comments,
        NotificationType.FriendRequestReceived or NotificationType.FriendRequestAccepted => FriendRequests,
        NotificationType.StoryMentioned or NotificationType.StoryReshared => StoryTags,
        _ => true
    };
}
