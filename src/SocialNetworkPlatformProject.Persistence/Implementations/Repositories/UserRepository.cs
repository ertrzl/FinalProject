using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Identity;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class UserRepository : IUserRepository
{
    private static readonly Expression<Func<ApplicationUser, UserSummary>> ToSummary = u => new UserSummary
    {
        Id = u.Id,
        FullName = u.FullName,
        UserName = u.UserName ?? string.Empty,
        AvatarUrl = u.AvatarUrl,
        CoverPhotoUrl = u.CoverPhotoUrl,
        Bio = u.Bio,
        Location = u.Location,
        Occupation = u.Occupation,
        Education = u.Education,
        IsPrivateAccount = u.IsPrivateAccount,
        ShowOnlineStatus = u.ShowOnlineStatus,
        ShowFriendsList = u.ShowFriendsList,
        IsSuspended = u.IsSuspended,
        LastSeenAt = u.LastSeenAt,
        CreatedAt = u.CreatedAt
    };

    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserSummary?> GetSummaryAsync(Guid id)
    {
        return await _context.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(ToSummary)
            .FirstOrDefaultAsync();
    }

    public async Task<Dictionary<Guid, UserSummary>> GetSummariesAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
            return new Dictionary<Guid, UserSummary>();

        return await _context.Users.AsNoTracking()
            .Where(u => idList.Contains(u.Id))
            .Select(ToSummary)
            .ToDictionaryAsync(u => u.Id);
    }

    public async Task<Dictionary<string, UserSummary>> GetSummariesByUserNamesAsync(IEnumerable<string> userNames)
    {
        // Identity keeps an upper-cased copy of every user name; looking it up is case-insensitive and uses its index.
        var normalized = userNames.Select(name => name.ToUpperInvariant()).Distinct().ToList();
        if (normalized.Count == 0)
            return new Dictionary<string, UserSummary>();

        var users = await _context.Users.AsNoTracking()
            .Where(u => normalized.Contains(u.NormalizedUserName!))
            .Select(ToSummary)
            .ToListAsync();

        return users.ToDictionary(u => u.UserName.ToLowerInvariant());
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Users.AnyAsync(u => u.Id == id);
    }

    public async Task<string?> GetSecurityStampAsync(Guid id)
    {
        return await _context.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => u.SecurityStamp)
            .FirstOrDefaultAsync();
    }

    public async Task<NotificationPreferences?> GetNotificationPreferencesAsync(Guid id)
    {
        return await _context.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new NotificationPreferences(u.NotifyOnLikes, u.NotifyOnComments, u.NotifyOnFriendRequests))
            .FirstOrDefaultAsync();
    }

    public async Task<(List<UserSummary> Items, int TotalCount)> SearchAsync(
        string term, Guid currentUserId, IReadOnlyCollection<Guid> excludeIds, int page, int pageSize)
    {
        // The current user is deliberately included: typing your own name should still find your own profile.
        var query = _context.Users.AsNoTracking()
            .Where(u => (u.FullName.Contains(term) || u.UserName!.Contains(term)) && !excludeIds.Contains(u.Id));

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(u => u.FullName)
            .ThenBy(u => u.Id) // two people with the same name must not swap places between pages
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToSummary)
            .ToListAsync();

        return (items, total);
    }

    public async Task<List<UserSummary>> GetRecentAsync(IEnumerable<Guid> excludeIds, int take)
    {
        var excluded = excludeIds.ToList();

        return await _context.Users.AsNoTracking()
            .Where(u => !excluded.Contains(u.Id))
            .OrderByDescending(u => u.CreatedAt)
            .Take(take)
            .Select(ToSummary)
            .ToListAsync();
    }

    public async Task TouchLastSeenAsync(Guid id)
    {
        var now = DateTime.UtcNow;
        await _context.Users
            .Where(u => u.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.LastSeenAt, now));
    }

    public async Task<int> CountAsync(bool? suspended = null, DateTime? createdSince = null)
    {
        var query = _context.Users.AsQueryable();
        if (suspended.HasValue)
            query = query.Where(u => u.IsSuspended == suspended.Value);
        if (createdSince.HasValue)
            query = query.Where(u => u.CreatedAt >= createdSince.Value);

        return await query.CountAsync();
    }

    public async Task<(List<AdminUserRow> Items, int TotalCount)> SearchForAdminAsync(string? term, bool? suspended, int page, int pageSize)
    {
        var query = _context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(term))
        {
            var text = term.Trim();
            query = query.Where(u => u.FullName.Contains(text) || u.UserName!.Contains(text) || u.Email!.Contains(text));
        }

        if (suspended.HasValue)
            query = query.Where(u => u.IsSuspended == suspended.Value);

        var total = await query.CountAsync();
        var found = await query
            .OrderByDescending(u => u.CreatedAt)
            .ThenBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var ids = found.Select(u => u.Id).ToList();
        var adminRoleId = await _context.Roles.Where(r => r.Name == AppRoles.Admin).Select(r => (Guid?)r.Id).FirstOrDefaultAsync();
        var adminIds = adminRoleId == null
            ? new HashSet<Guid>()
            : (await _context.UserRoles.Where(ur => ur.RoleId == adminRoleId && ids.Contains(ur.UserId)).Select(ur => ur.UserId).ToListAsync()).ToHashSet();

        var rows = found.Select(u => new AdminUserRow
        {
            Id = u.Id,
            FullName = u.FullName,
            UserName = u.UserName ?? string.Empty,
            Email = u.Email ?? string.Empty,
            AvatarUrl = u.AvatarUrl,
            CreatedAt = u.CreatedAt,
            LastSeenAt = u.LastSeenAt,
            IsAdmin = adminIds.Contains(u.Id),
            IsSuspended = u.IsSuspended,
            SuspendedAt = u.SuspendedAt,
            SuspendReason = u.SuspendReason
        }).ToList();

        return (rows, total);
    }

    public async Task<List<string>> DeleteUserWithDataAsync(Guid userId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var myPosts = _context.Set<Post>().Where(p => p.AuthorId == userId);
        var myPostIds = myPosts.Select(p => p.Id);
        var myComments = _context.Set<Comment>().Where(c => c.AuthorId == userId);
        var myCommentIds = myComments.Select(c => c.Id);
        var myConversationIds = _context.Set<ConversationParticipant>()
            .Where(p => p.UserId == userId)
            .Select(p => p.ConversationId);

        // The groups the person owns. A group with other members lives on: it is handed to the member best placed to run
        // it (an admin, else a moderator, else whoever joined first), so the others keep their posts, events and
        // discussions. Only a group nobody else belongs to goes with the account.
        var ownedGroups = await _context.Set<Group>().Include(g => g.Members).Where(g => g.CreatedByUserId == userId).ToListAsync();
        var groupIdsToDelete = new List<Guid>();
        foreach (var group in ownedGroups)
        {
            var heir = group.Members
                .Where(m => m.UserId != userId)
                .OrderBy(m => m.Role == GroupMemberRole.Admin ? 0 : m.Role == GroupMemberRole.Moderator ? 1 : 2)
                .ThenBy(m => m.CreatedAt)
                .FirstOrDefault();

            if (heir == null)
            {
                groupIdsToDelete.Add(group.Id);
                continue;
            }

            heir.Role = GroupMemberRole.Admin;
            group.CreatedByUserId = heir.UserId;
        }

        // Events the person organized inside a group that stays are handed to that group's owner (as when leaving a group).
        var eventsToHandOver = await _context.Set<Event>()
            .Include(e => e.Group)
            .Where(e => e.GroupId != null && e.CreatedByUserId == userId && !groupIdsToDelete.Contains(e.GroupId.Value))
            .ToListAsync();
        foreach (var groupEvent in eventsToHandOver)
            groupEvent.CreatedByUserId = groupEvent.Group!.CreatedByUserId;

        await _context.SaveChangesAsync();

        // What goes: events the person organized outside any group, and every event of a group that goes.
        var myEvents = _context.Set<Event>().Where(e =>
            (e.GroupId == null && e.CreatedByUserId == userId) || (e.GroupId != null && groupIdsToDelete.Contains(e.GroupId.Value)));
        var myEventIds = myEvents.Select(e => e.Id);

        // Every post on the wall of a group that goes (other people's included) goes with it.
        var deletedGroupPostIds = _context.Set<Post>()
            .Where(p => p.GroupId != null && groupIdsToDelete.Contains(p.GroupId.Value))
            .Select(p => p.Id);

        // Files that will be orphaned once the rows below are gone.
        var files = new List<string>();
        var profile = await _context.Users.Where(u => u.Id == userId).Select(u => new { u.AvatarUrl, u.CoverPhotoUrl }).FirstOrDefaultAsync();
        if (profile?.AvatarUrl != null) files.Add(profile.AvatarUrl);
        if (profile?.CoverPhotoUrl != null) files.Add(profile.CoverPhotoUrl);
        files.AddRange(await _context.Set<PostMedia>().Where(m => m.Post!.AuthorId == userId).Select(m => m.Url).ToListAsync());
        files.AddRange(await _context.Set<PostMedia>().Where(m => deletedGroupPostIds.Contains(m.PostId)).Select(m => m.Url).ToListAsync());
        files.AddRange(await _context.Set<Story>().Where(s => s.UserId == userId).Select(s => s.MediaUrl).ToListAsync());
        files.AddRange(await _context.Set<ListingImage>().Where(i => i.Listing!.SellerId == userId).Select(i => i.Url).ToListAsync());
        // The photos of the person's chats: the conversations go with the account, so both sides' messages do.
        files.AddRange(await _context.Set<Message>().Where(m => myConversationIds.Contains(m.ConversationId) && m.MediaUrl != null).Select(m => m.MediaUrl!).ToListAsync());
        files.AddRange(await _context.Set<Group>().Where(g => groupIdsToDelete.Contains(g.Id) && g.CoverImageUrl != null).Select(g => g.CoverImageUrl!).ToListAsync());
        files.AddRange(await myEvents.Where(e => e.CoverImageUrl != null).Select(e => e.CoverImageUrl!).ToListAsync());

        await _context.Set<Notification>()
            .Where(n => n.RecipientId == userId || n.ActorId == userId
                || (n.PostId != null && (myPostIds.Contains(n.PostId.Value) || deletedGroupPostIds.Contains(n.PostId.Value)))
                || (n.EventId != null && myEventIds.Contains(n.EventId.Value))
                || (n.GroupId != null && groupIdsToDelete.Contains(n.GroupId.Value)))
            .ExecuteDeleteAsync();

        await _context.Set<PostLike>().Where(l => l.UserId == userId).ExecuteDeleteAsync();
        await _context.Set<PollVote>().Where(v => v.UserId == userId).ExecuteDeleteAsync();
        await _context.Set<CommentLike>().Where(l => l.UserId == userId).ExecuteDeleteAsync();
        await _context.Set<SavedPost>().Where(s => s.UserId == userId).ExecuteDeleteAsync();

        // Replies to the user's comments first: the self-reference is Restrict, not cascade.
        await _context.Set<Comment>()
            .Where(c => c.ParentCommentId != null && myCommentIds.Contains(c.ParentCommentId.Value))
            .ExecuteDeleteAsync();
        await myComments.ExecuteDeleteAsync();

        // Other people's shares of these posts stop pointing at them (a self-reference cannot null itself in SQL Server);
        // the user's own shares go with the user's posts below.
        await _context.Set<Post>()
            .Where(p => p.SharedPostId != null && myPostIds.Contains(p.SharedPostId.Value))
            .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.SharedPostId, (Guid?)null));

        await myPosts.ExecuteDeleteAsync();

        await _context.Set<FriendRequest>().Where(r => r.SenderId == userId || r.ReceiverId == userId).ExecuteDeleteAsync();
        await _context.Set<Friendship>().Where(f => f.UserOneId == userId || f.UserTwoId == userId).ExecuteDeleteAsync();
        await _context.Set<UserBlock>().Where(b => b.BlockerId == userId || b.BlockedId == userId).ExecuteDeleteAsync();

        // The user's own stories take their view and like rows with them (cascade); the ones they left on others' stories don't.
        await _context.Set<StoryView>().Where(v => v.ViewerId == userId).ExecuteDeleteAsync();
        await _context.Set<StoryLike>().Where(l => l.UserId == userId).ExecuteDeleteAsync();
        await _context.Set<Story>().Where(s => s.UserId == userId).ExecuteDeleteAsync();

        await _context.Set<MessageReaction>().Where(r => r.UserId == userId).ExecuteDeleteAsync(); // emojis left on other people's messages (their own messages go with the conversations)
        await _context.Set<Conversation>().Where(c => myConversationIds.Contains(c.Id)).ExecuteDeleteAsync();

        // Games, invitations and finished ones alike: the opponent has nobody left to play or answer.
        await _context.Set<Game>().Where(g => g.InviterId == userId || g.InviteeId == userId).ExecuteDeleteAsync();

        // Marketplace trail: reviews written by or about the user, their offers on other people's listings
        // (their own listings' offers go with the listings) and their favourites.
        await _context.Set<SellerRating>().Where(r => r.BuyerId == userId || r.SellerId == userId).ExecuteDeleteAsync();
        await _context.Set<ListingOffer>().Where(o => o.BuyerId == userId).ExecuteDeleteAsync();
        await _context.Set<SavedListing>().Where(s => s.UserId == userId).ExecuteDeleteAsync();
        await _context.Set<MarketplaceListing>().Where(l => l.SellerId == userId).ExecuteDeleteAsync();

        await _context.Set<EventComment>().Where(c => c.AuthorId == userId).ExecuteDeleteAsync();
        await _context.Set<EventInvite>().Where(i => i.InvitedUserId == userId || i.InvitedByUserId == userId).ExecuteDeleteAsync();
        await _context.Set<EventAttendee>().Where(a => a.UserId == userId).ExecuteDeleteAsync();
        await _context.Set<Event>().Where(e => e.GroupId == null && e.CreatedByUserId == userId).ExecuteDeleteAsync();

        // Groups: the person's membership, the requests and invitations they made or got (in the groups that stay), then
        // the groups that go (with their members, wall posts and events, by cascade).
        await _context.Set<GroupMember>().Where(m => m.UserId == userId).ExecuteDeleteAsync();
        await _context.Set<GroupJoinRequest>().Where(r => r.UserId == userId).ExecuteDeleteAsync();
        await _context.Set<GroupInvite>().Where(i => i.UserId == userId || i.InvitedByUserId == userId).ExecuteDeleteAsync();
        await _context.Set<Group>().Where(g => groupIdsToDelete.Contains(g.Id)).ExecuteDeleteAsync();

        // Reports: the ones the person made, and the ones about them or about anything of theirs (the content is gone, there is
        // nothing left for the admins to look at). The reports an admin closed stay when that admin's account goes; only the
        // name of who closed them is dropped. (The admin log keeps its own copy of every name, so it is left as it is.)
        await _context.Set<Report>().Where(r => r.ReporterId == userId || r.TargetOwnerId == userId).ExecuteDeleteAsync();
        await _context.Set<Report>().Where(r => r.ResolvedById == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.ResolvedById, (Guid?)null));

        await _context.Set<RefreshToken>().Where(t => t.UserId == userId).ExecuteDeleteAsync();
        await _context.Users.Where(u => u.Id == userId).ExecuteDeleteAsync();

        await transaction.CommitAsync();

        return files;
    }
}
