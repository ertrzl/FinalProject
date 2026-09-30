using Microsoft.EntityFrameworkCore;
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
    private readonly IGroupRepository _groups;
    private readonly IGroupMemberRepository _groupMembers;

    public PostAccessService(
        IFriendService friends,
        IUserRepository users,
        IGroupRepository groups,
        IGroupMemberRepository groupMembers)
    {
        _friends = friends;
        _users = users;
        _groups = groups;
        _groupMembers = groupMembers;
    }

    public async Task<bool> CanViewAsync(Post post, Guid viewerId)
    {
        if (post.GroupId.HasValue)
            return await CanViewGroupPostAsync(post.GroupId.Value, viewerId);

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

        var personal = list.Where(p => !p.GroupId.HasValue).ToList();
        var groupPosts = list.Where(p => p.GroupId.HasValue).ToList();
        var visible = new List<Post>();

        if (personal.Count > 0)
        {
            var friendIds = (await _friends.GetFriendIdsAsync(viewerId)).ToHashSet();
            var authors = await _users.GetSummariesAsync(personal.Select(p => p.AuthorId));

            visible.AddRange(personal.Where(p =>
                p.AuthorId == viewerId ||
                friendIds.Contains(p.AuthorId) ||
                (p.Privacy == PostPrivacy.Public && authors.TryGetValue(p.AuthorId, out var author) && !author.IsPrivateAccount)));
        }

        if (groupPosts.Count > 0)
        {
            var groupIds = groupPosts.Select(p => p.GroupId!.Value).Distinct().ToList();
            var groups = await _groups.GetAll(g => groupIds.Contains(g.Id), asNoTracking: true).ToDictionaryAsync(g => g.Id);
            var myGroupIds = (await _groupMembers.GetAll(m => m.UserId == viewerId && groupIds.Contains(m.GroupId), asNoTracking: true)
                    .Select(m => m.GroupId)
                    .ToListAsync())
                .ToHashSet();

            visible.AddRange(groupPosts.Where(p =>
                groups.TryGetValue(p.GroupId!.Value, out var group) &&
                (group.Privacy == GroupPrivacy.Public || myGroupIds.Contains(p.GroupId.Value))));
        }

        return visible;
    }

    private async Task<bool> CanViewGroupPostAsync(Guid groupId, Guid viewerId)
    {
        var group = await _groups.GetByIdAsync(groupId);
        if (group == null)
            return false;

        if (group.Privacy == GroupPrivacy.Public)
            return true;

        return await _groupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == viewerId);
    }
}
