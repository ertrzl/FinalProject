using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.DTOs.Comments;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class LiveUpdateService : ILiveUpdateService
{
    private readonly IRealTimeNotifier _notifier;
    private readonly IFriendService _friends;
    private readonly IGroupMemberRepository _groupMembers;
    private readonly IUserRepository _users;
    private readonly IPresenceTracker _presence;

    public LiveUpdateService(
        IRealTimeNotifier notifier,
        IFriendService friends,
        IGroupMemberRepository groupMembers,
        IUserRepository users,
        IPresenceTracker presence)
    {
        _notifier = notifier;
        _friends = friends;
        _groupMembers = groupMembers;
        _users = users;
        _presence = presence;
    }

    public Task PostCreatedAsync(Post post)
    {
        return PublishToAudienceAsync(post, "PostCreated", new { postId = post.Id });
    }

    public Task PostUpdatedAsync(Post post)
    {
        return PublishToAudienceAsync(post, "PostUpdated", new { postId = post.Id });
    }

    public Task PostDeletedAsync(Post post)
    {
        return PublishToAudienceAsync(post, "PostDeleted", new { postId = post.Id });
    }

    public Task PostLikeCountChangedAsync(Post post, Guid actorId, int likeCount)
    {
        return PublishToAudienceAsync(post, "PostLikeCountChanged", new { postId = post.Id, likeCount }, actorId);
    }

    public Task CommentAddedAsync(Post post, GetCommentDto comment, int commentCount)
    {
        // The same comment goes to everybody, so it carries nothing that depends on who is looking (liked by me?
        // may I delete it?): each page asks for those itself.
        var shared = new
        {
            comment.Id, comment.PostId, comment.AuthorId, comment.AuthorName, comment.AuthorAvatarUrl,
            comment.Text, comment.CreatedAt, comment.ParentCommentId, comment.LikeCount
        };

        return PublishToAudienceAsync(post, "CommentAdded", new { postId = post.Id, commentCount, comment = shared }, comment.AuthorId);
    }

    public Task CommentDeletedAsync(Post post, Guid actorId, IReadOnlyCollection<Guid> commentIds, int commentCount)
    {
        return PublishToAudienceAsync(post, "CommentDeleted", new { postId = post.Id, commentIds, commentCount }, actorId);
    }

    public Task CommentLikeCountChangedAsync(Post post, Guid actorId, Guid commentId, int likeCount)
    {
        return PublishToAudienceAsync(post, "CommentLikeCountChanged", new { postId = post.Id, commentId, likeCount }, actorId);
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

    private async Task PublishToAudienceAsync(Post post, string eventName, object payload, params Guid[] alsoInclude)
    {
        try
        {
            // A group post is only ever shown to the group's members, so that is who hears about it.
            var audience = post.GroupId is { } groupId
                ? (await _groupMembers.GetAll(m => m.GroupId == groupId, asNoTracking: true).Select(m => m.UserId).ToListAsync()).ToHashSet()
                : new HashSet<Guid>(await _friends.GetFriendIdsAsync(post.AuthorId));

            audience.Add(post.AuthorId);
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
