using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class PostAccessService : IPostAccessService
{
    private readonly IFriendService _friends;
    private readonly IUserRepository _users;

    public PostAccessService(IFriendService friends, IUserRepository users)
    {
        _friends = friends;
        _users = users;
    }

    public async Task<bool> CanViewAsync(Post post, Guid viewerId)
    {
        if (post.AuthorId == viewerId)
            return true;

        if (await _friends.AreFriendsAsync(post.AuthorId, viewerId))
            return true;

        if (post.Privacy != PostPrivacy.Public)
            return false;

        var author = await _users.GetSummaryAsync(post.AuthorId);
        return author is { IsPrivateAccount: false };
    }

    public async Task EnsureCanViewAsync(Post post, Guid viewerId)
    {
        if (!await CanViewAsync(post, viewerId))
            throw new NotFoundException("Post not found.");
    }

    public async Task<List<Post>> FilterVisibleAsync(IEnumerable<Post> posts, Guid viewerId)
    {
        var list = posts.ToList();
        if (list.Count == 0)
            return list;

        var friendIds = (await _friends.GetFriendIdsAsync(viewerId)).ToHashSet();
        var authors = await _users.GetSummariesAsync(list.Select(p => p.AuthorId));

        return list.Where(p =>
                p.AuthorId == viewerId ||
                friendIds.Contains(p.AuthorId) ||
                (p.Privacy == PostPrivacy.Public && authors.TryGetValue(p.AuthorId, out var author) && !author.IsPrivateAccount))
            .ToList();
    }
}
