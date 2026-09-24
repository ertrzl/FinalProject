using SocialNetworkPlatformProject.Application.DTOs.Comments;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// Pushes feed/presence changes to open browsers over SignalR. Best effort: a failed push never fails the request that caused it.
// Post and comment events go to the post author's friends plus the author and the acting user (their other tabs) — the feed audience.
public interface ILiveUpdateService
{
    Task PostCreatedAsync(Guid authorId, Guid postId);

    Task PostUpdatedAsync(Guid authorId, Guid postId);

    Task PostDeletedAsync(Guid authorId, Guid postId);

    Task PostLikeCountChangedAsync(Guid authorId, Guid actorId, Guid postId, int likeCount);

    Task CommentAddedAsync(Guid postAuthorId, GetCommentDto comment, int commentCount);

    Task CommentDeletedAsync(Guid postAuthorId, Guid actorId, Guid postId, IReadOnlyCollection<Guid> commentIds, int commentCount);

    Task CommentLikeCountChangedAsync(Guid postAuthorId, Guid actorId, Guid postId, Guid commentId, int likeCount);

    // Tells every client whether the user is online (always "offline" for users who hide their status).
    Task PresenceChangedAsync(Guid userId);
}
