using SocialNetworkPlatformProject.Application.DTOs.Comments;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// Pushes feed/presence changes to open browsers over SignalR. Best effort: a failed push never fails the request that caused it.
// Post and comment events go to the post's feed audience plus the author and the acting user (their other tabs):
// the author's friends for a personal post, the members of the group for a group post (a private group's
// comments must never reach people outside it).
public interface ILiveUpdateService
{
    Task PostCreatedAsync(Post post);

    Task PostUpdatedAsync(Post post);

    Task PostDeletedAsync(Post post);

    Task PostLikeCountChangedAsync(Post post, Guid actorId, int likeCount);

    Task CommentAddedAsync(Post post, GetCommentDto comment, int commentCount);

    Task CommentDeletedAsync(Post post, Guid actorId, IReadOnlyCollection<Guid> commentIds, int commentCount);

    Task CommentLikeCountChangedAsync(Post post, Guid actorId, Guid commentId, int likeCount);

    // Tells every client whether the user is online (always "offline" for users who hide their status).
    Task PresenceChangedAsync(Guid userId);
}
