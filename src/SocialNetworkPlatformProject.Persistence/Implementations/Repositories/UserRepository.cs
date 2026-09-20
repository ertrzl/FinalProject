using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
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

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Users.AnyAsync(u => u.Id == id);
    }

    public async Task<(List<UserSummary> Items, int TotalCount)> SearchAsync(string term, Guid excludeUserId, int page, int pageSize)
    {
        var query = _context.Users.AsNoTracking()
            .Where(u => u.Id != excludeUserId && (u.FullName.Contains(term) || u.UserName!.Contains(term)));

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(u => u.FullName)
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

    public async Task<List<string>> DeleteAllUserDataAsync(Guid userId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var myPosts = _context.Set<Post>().Where(p => p.AuthorId == userId);
        var myPostIds = myPosts.Select(p => p.Id);
        var myComments = _context.Set<Comment>().Where(c => c.AuthorId == userId);
        var myCommentIds = myComments.Select(c => c.Id);
        var myConversationIds = _context.Set<ConversationParticipant>()
            .Where(p => p.UserId == userId)
            .Select(p => p.ConversationId);

        // Image files that will be orphaned once the rows below are gone.
        var files = new List<string>();
        files.AddRange(await myPosts.Where(p => p.ImageUrl != null).Select(p => p.ImageUrl!).ToListAsync());
        files.AddRange(await _context.Set<Story>().Where(s => s.UserId == userId).Select(s => s.ImageUrl).ToListAsync());
        files.AddRange(await _context.Set<MarketplaceListing>().Where(l => l.SellerId == userId && l.ImageUrl != null).Select(l => l.ImageUrl!).ToListAsync());
        files.AddRange(await _context.Set<Group>().Where(g => g.CreatedByUserId == userId && g.CoverImageUrl != null).Select(g => g.CoverImageUrl!).ToListAsync());
        files.AddRange(await _context.Set<Event>().Where(e => e.CreatedByUserId == userId && e.CoverImageUrl != null).Select(e => e.CoverImageUrl!).ToListAsync());

        await _context.Set<Notification>()
            .Where(n => n.RecipientId == userId || n.ActorId == userId || (n.PostId != null && myPostIds.Contains(n.PostId.Value)))
            .ExecuteDeleteAsync();

        await _context.Set<PostLike>().Where(l => l.UserId == userId).ExecuteDeleteAsync();
        await _context.Set<CommentLike>().Where(l => l.UserId == userId).ExecuteDeleteAsync();
        await _context.Set<SavedPost>().Where(s => s.UserId == userId).ExecuteDeleteAsync();

        // Replies to the user's comments first: the self-reference is Restrict, not cascade.
        await _context.Set<Comment>()
            .Where(c => c.ParentCommentId != null && myCommentIds.Contains(c.ParentCommentId.Value))
            .ExecuteDeleteAsync();
        await myComments.ExecuteDeleteAsync();

        await myPosts.ExecuteDeleteAsync();

        await _context.Set<FriendRequest>().Where(r => r.SenderId == userId || r.ReceiverId == userId).ExecuteDeleteAsync();
        await _context.Set<Friendship>().Where(f => f.UserOneId == userId || f.UserTwoId == userId).ExecuteDeleteAsync();

        await _context.Set<Story>().Where(s => s.UserId == userId).ExecuteDeleteAsync();

        await _context.Set<Conversation>().Where(c => myConversationIds.Contains(c.Id)).ExecuteDeleteAsync();

        await _context.Set<MarketplaceListing>().Where(l => l.SellerId == userId).ExecuteDeleteAsync();

        await _context.Set<EventAttendee>().Where(a => a.UserId == userId).ExecuteDeleteAsync();
        await _context.Set<Event>().Where(e => e.CreatedByUserId == userId).ExecuteDeleteAsync();

        await _context.Set<GroupMember>().Where(m => m.UserId == userId).ExecuteDeleteAsync();
        await _context.Set<Group>().Where(g => g.CreatedByUserId == userId).ExecuteDeleteAsync();

        await transaction.CommitAsync();

        return files;
    }
}
