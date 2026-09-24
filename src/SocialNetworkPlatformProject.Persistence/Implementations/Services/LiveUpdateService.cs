using SocialNetworkPlatformProject.Application.DTOs.Comments;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class LiveUpdateService : ILiveUpdateService
{
    private readonly IRealTimeNotifier _notifier;
    private readonly IFriendService _friends;
    private readonly IUserRepository _users;
    private readonly IPresenceTracker _presence;

    public LiveUpdateService(IRealTimeNotifier notifier, IFriendService friends, IUserRepository users, IPresenceTracker presence)
    {
        _notifier = notifier;
        _friends = friends;
        _users = users;
        _presence = presence;
    }

    public Task PostCreatedAsync(Guid authorId, Guid postId)
    {
        return PublishToAudienceAsync(authorId, "PostCreated", new { postId });
    }

    public Task PostUpdatedAsync(Guid authorId, Guid postId)
    {
        return PublishToAudienceAsync(authorId, "PostUpdated", new { postId });
    }

    public Task PostDeletedAsync(Guid authorId, Guid postId)
    {
        return PublishToAudienceAsync(authorId, "PostDeleted", new { postId });
    }

    public Task PostLikeCountChangedAsync(Guid authorId, Guid actorId, Guid postId, int likeCount)
    {
        return PublishToAudienceAsync(authorId, "PostLikeCountChanged", new { postId, likeCount }, actorId);
    }

    public Task CommentAddedAsync(Guid postAuthorId, GetCommentDto comment, int commentCount)
    {
        return PublishToAudienceAsync(postAuthorId, "CommentAdded",
            new { postId = comment.PostId, commentCount, comment }, comment.AuthorId);
    }

    public Task CommentDeletedAsync(Guid postAuthorId, Guid actorId, Guid postId, IReadOnlyCollection<Guid> commentIds, int commentCount)
    {
        return PublishToAudienceAsync(postAuthorId, "CommentDeleted", new { postId, commentIds, commentCount }, actorId);
    }

    public Task CommentLikeCountChangedAsync(Guid postAuthorId, Guid actorId, Guid postId, Guid commentId, int likeCount)
    {
        return PublishToAudienceAsync(postAuthorId, "CommentLikeCountChanged", new { postId, commentId, likeCount }, actorId);
    }

    public async Task PresenceChangedAsync(Guid userId)
    {
        try
        {
            var user = await _users.GetSummaryAsync(userId);
            if (user == null)
                return;

            var isOnline = user.ShowOnlineStatus && _presence.IsOnline(userId);
            await _notifier.BroadcastAsync("PresenceChanged", new { userId, isOnline });
        }
        catch (Exception)
        {
            // Live pushes are best effort: the page still shows the right state after its next load.
        }
    }

    private async Task PublishToAudienceAsync(Guid authorId, string eventName, object payload, params Guid[] alsoInclude)
    {
        try
        {
            var audience = new HashSet<Guid>(await _friends.GetFriendIdsAsync(authorId)) { authorId };
            foreach (var id in alsoInclude)
                audience.Add(id);

            await _notifier.PublishAsync(audience, eventName, payload);
        }
        catch (Exception)
        {
            // Live pushes are best effort: the request that caused this must not fail because of them.
        }
    }
}
