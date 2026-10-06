using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;
using SocialNetworkPlatformProject.Persistence.Contexts;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class PostAccessService : IPostAccessService
{
    private readonly IFriendService _friends;
    private readonly IUserRepository _users;
    private readonly IGroupRepository _groups;
    private readonly IGroupMemberRepository _groupMembers;
    private readonly ApplicationDbContext _context;

    public PostAccessService(
        IFriendService friends,
        IUserRepository users,
        IGroupRepository groups,
        IGroupMemberRepository groupMembers,
        ApplicationDbContext context)
    {
        _friends = friends;
        _users = users;
        _groups = groups;
        _groupMembers = groupMembers;
        _context = context;
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

    public IQueryable<Post> VisibleTo(IQueryable<Post> posts, Guid viewerId)
    {
        var friendships = _context.Set<Friendship>();
        var users = _context.Users;
        var memberships = _context.Set<GroupMember>();

        return posts.Where(p => p.GroupId == null
            // A personal post: its author, the author's friends, or anyone if it is public and the account is open.
            ? p.AuthorId == viewerId
                || friendships.Any(f => (f.UserOneId == viewerId && f.UserTwoId == p.AuthorId) || (f.UserTwoId == viewerId && f.UserOneId == p.AuthorId))
                || (p.Privacy == PostPrivacy.Public && users.Any(u => u.Id == p.AuthorId && !u.IsPrivateAccount))
            // A group post: anyone for a public group, otherwise only its members.
            : p.Group!.Privacy == GroupPrivacy.Public
                || memberships.Any(m => m.GroupId == p.GroupId && m.UserId == viewerId));
    }

    public async Task<bool> CanModerateAsync(Post post, Guid userId)
    {
        if (post.AuthorId == userId)
            return true;

        if (!post.GroupId.HasValue)
            return false;

        return await _groupMembers.AnyAsync(m => m.GroupId == post.GroupId.Value && m.UserId == userId
            && (m.Role == GroupMemberRole.Admin || m.Role == GroupMemberRole.Moderator));
    }

    public async Task<HashSet<Guid>> GetModeratablePostIdsAsync(IReadOnlyCollection<Post> posts, Guid userId)
    {
        var groupIds = posts.Where(p => p.GroupId.HasValue).Select(p => p.GroupId!.Value).Distinct().ToList();

        var moderatedGroupIds = groupIds.Count == 0
            ? new HashSet<Guid>()
            : (await _groupMembers.GetAll(
                    m => m.UserId == userId && groupIds.Contains(m.GroupId)
                        && (m.Role == GroupMemberRole.Admin || m.Role == GroupMemberRole.Moderator),
                    asNoTracking: true)
                .Select(m => m.GroupId)
                .ToListAsync()).ToHashSet();

        return posts
            .Where(p => p.AuthorId == userId || (p.GroupId.HasValue && moderatedGroupIds.Contains(p.GroupId.Value)))
            .Select(p => p.Id)
            .ToHashSet();
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
