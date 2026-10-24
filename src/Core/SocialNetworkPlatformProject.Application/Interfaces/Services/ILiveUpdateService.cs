using SocialNetworkPlatformProject.Application.DTOs.Comments;
using SocialNetworkPlatformProject.Application.DTOs.Posts;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface ILiveUpdateService
{
    Task PostCreatedAsync(Post post);

    Task PostUpdatedAsync(Post post);

    Task PostDeletedAsync(Post post);

    Task PostLikeCountChangedAsync(Post post, Guid actorId, int likeCount, IReadOnlyCollection<GetReactionCountDto> reactions);

    Task PollVotedAsync(Post post, Guid actorId);

    Task CommentAddedAsync(Post post, GetCommentDto comment, int commentCount);

    Task CommentDeletedAsync(Post post, Guid actorId, IReadOnlyCollection<Guid> commentIds, int commentCount);

    Task CommentLikeCountChangedAsync(Post post, Guid actorId, Guid commentId, int likeCount);

    Task PresenceChangedAsync(Guid userId);
}
