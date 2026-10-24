using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SocialNetworkPlatformProject.Application.DTOs.Comments;
using SocialNetworkPlatformProject.Application.DTOs.Posts;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class LiveUpdateService : ILiveUpdateService
{
    private readonly IRealTimeNotifier _notifier;
    private readonly IFriendService _friends;
    private readonly IBlockService _blocks;
    private readonly IGroupMemberRepository _groupMembers;
    private readonly IUserRepository _users;
    private readonly IPresenceTracker _presence;
    private readonly IConversationParticipantRepository _participants;
    private readonly ILogger<LiveUpdateService> _logger;

    public LiveUpdateService(
        IRealTimeNotifier notifier,
        IFriendService friends,
        IBlockService blocks,
        IGroupMemberRepository groupMembers,
        IUserRepository users,
        IPresenceTracker presence,
        IConversationParticipantRepository participants,
        ILogger<LiveUpdateService> logger)
    {
        _notifier = notifier;
        _friends = friends;
        _blocks = blocks;
        _groupMembers = groupMembers;
        _users = users;
        _presence = presence;
        _participants = participants;
        _logger = logger;
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

    public Task PostLikeCountChangedAsync(Post post, Guid actorId, int likeCount, IReadOnlyCollection<GetReactionCountDto> reactions)
    {
        return PublishToAudienceAsync(post, "PostLikeCountChanged", new { postId = post.Id, likeCount, reactions }, actorId);
    }

    public Task PollVotedAsync(Post post, Guid actorId)
    {
        return PublishToAudienceAsync(post, "PollVoted", new { postId = post.Id }, actorId);
    }

    public Task CommentAddedAsync(Post post, GetCommentDto comment, int commentCount)
    {
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

            var audience = new HashSet<Guid>(await _friends.GetFriendIdsAsync(userId));

            var myConversationIds = _participants.GetAll(p => p.UserId == userId, asNoTracking: true).Select(p => p.ConversationId);
            audience.UnionWith(await _participants
                .GetAll(p => myConversationIds.Contains(p.ConversationId) && p.UserId != userId, asNoTracking: true)
                .Select(p => p.UserId)
                .ToListAsync());

            audience.ExceptWith(await _blocks.GetBlockedIdsAsync(userId));
            audience.Remove(userId);
            if (audience.Count == 0)
                return;

            await _notifier.PublishAsync(audience, "PresenceChanged", new { userId, isOnline });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "The online status of user {UserId:l} could not be pushed", userId.ToString());
        }
    }

    private async Task PublishToAudienceAsync(Post post, string eventName, object payload, params Guid[] alsoInclude)
    {
        try
        {
            var audience = post.GroupId is { } groupId
                ? (await _groupMembers.GetAll(m => m.GroupId == groupId, asNoTracking: true).Select(m => m.UserId).ToListAsync()).ToHashSet()
                : new HashSet<Guid>(await _friends.GetFriendIdsAsync(post.AuthorId));

            audience.Add(post.AuthorId);
            foreach (var id in alsoInclude)
                audience.Add(id);

            foreach (var id in alsoInclude.Append(post.AuthorId))
                audience.ExceptWith(await _blocks.GetBlockedIdsAsync(id));

            await _notifier.PublishAsync(audience, eventName, payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "The live update {EventName} of post {PostId:l} could not be pushed", eventName, post.Id.ToString());
        }
    }
}
