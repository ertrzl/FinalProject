using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.DTOs.Admin;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Enums;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Identity;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class AdminService : IAdminService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserRepository _users;
    private readonly IAccessTokenRevoker _accessTokens;
    private readonly IFileStorageService _files;
    private readonly SessionIssuer _sessions;
    private readonly IPostService _posts;
    private readonly ICommentService _comments;
    private readonly IMarketplaceService _listings;
    private readonly IEventService _events;
    private readonly IGroupService _groups;
    private readonly IAuditService _audit;

    public AdminService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IUserRepository users,
        IAccessTokenRevoker accessTokens,
        IFileStorageService files,
        SessionIssuer sessions,
        IPostService posts,
        ICommentService comments,
        IMarketplaceService listings,
        IEventService events,
        IGroupService groups,
        IAuditService audit)
    {
        _context = context;
        _userManager = userManager;
        _users = users;
        _accessTokens = accessTokens;
        _files = files;
        _sessions = sessions;
        _posts = posts;
        _comments = comments;
        _listings = listings;
        _events = events;
        _groups = groups;
        _audit = audit;
    }

    public async Task<GetAdminStatsDto> GetStatsAsync()
    {
        var now = DateTime.UtcNow;
        var weekAgo = now.AddDays(-7);

        return new GetAdminStatsDto
        {
            Users = await _context.Users.CountAsync(),
            SuspendedUsers = await _context.Users.CountAsync(u => u.IsSuspended),
            NewUsersThisWeek = await _context.Users.CountAsync(u => u.CreatedAt >= weekAgo),
            Posts = await _context.Posts.CountAsync(),
            PostsToday = await _context.Posts.CountAsync(p => p.CreatedAt >= now.Date),
            Listings = await _context.MarketplaceListings.CountAsync(),
            Events = await _context.Events.CountAsync(),
            Groups = await _context.Groups.CountAsync(),
            PendingReports = await _context.Reports.CountAsync(r => r.Status == ReportStatus.Pending),
            ResolvedReports = await _context.Reports.CountAsync(r => r.Status != ReportStatus.Pending)
        };
    }

    public async Task<PagedResult<GetAdminUserDto>> SearchUsersAsync(string? term, bool? suspended, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

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
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var ids = found.Select(u => u.Id).ToList();

        var adminRoleId = await _context.Roles.Where(r => r.Name == AppRoles.Admin).Select(r => (Guid?)r.Id).FirstOrDefaultAsync();
        var adminIds = adminRoleId == null
            ? new HashSet<Guid>()
            : (await _context.UserRoles.Where(ur => ur.RoleId == adminRoleId && ids.Contains(ur.UserId)).Select(ur => ur.UserId).ToListAsync()).ToHashSet();

        var postCounts = await _context.Posts.Where(p => ids.Contains(p.AuthorId))
            .GroupBy(p => p.AuthorId).Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count);
        var reportCounts = await _context.Reports.Where(r => ids.Contains(r.TargetOwnerId))
            .GroupBy(r => r.TargetOwnerId).Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count);

        var items = found.Select(u => new GetAdminUserDto
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
            SuspendReason = u.SuspendReason,
            PostCount = postCounts.GetValueOrDefault(u.Id),
            ReportsAgainst = reportCounts.GetValueOrDefault(u.Id)
        }).ToList();

        return new PagedResult<GetAdminUserDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task SuspendUserAsync(Guid adminId, Guid userId, string? reason)
    {
        var user = await FindActableUserAsync(adminId, userId);

        if (user.IsSuspended)
            throw new ConflictException(Loc.T("admin.alreadySuspended"));

        var oldStamp = user.SecurityStamp!;
        user.IsSuspended = true;
        user.SuspendedAt = DateTime.UtcNow;
        user.SuspendReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        await _userManager.UpdateAsync(user);

        // The tokens in the person's browsers carry the old stamp: from the next request on they are refused, and
        // without refresh tokens no new ones can be made.
        await _userManager.UpdateSecurityStampAsync(user);
        _accessTokens.Revoke(userId, oldStamp);
        await _sessions.RevokeAllAsync(userId);

        await _audit.RecordAsync(adminId, "user.suspend", "User", userId, Describe(user, user.SuspendReason));
    }

    public async Task UnsuspendUserAsync(Guid adminId, Guid userId)
    {
        var user = await FindActableUserAsync(adminId, userId);

        if (!user.IsSuspended)
            throw new ConflictException(Loc.T("admin.notSuspended"));

        user.IsSuspended = false;
        user.SuspendedAt = null;
        user.SuspendReason = null;
        await _userManager.UpdateAsync(user);

        await _audit.RecordAsync(adminId, "user.unsuspend", "User", userId, Describe(user));
    }

    public async Task DeleteUserAsync(Guid adminId, Guid userId)
    {
        var user = await FindActableUserAsync(adminId, userId);
        var description = Describe(user);
        var stamp = user.SecurityStamp!;

        var orphanedFiles = await _users.DeleteUserWithDataAsync(userId);
        _accessTokens.Revoke(userId, stamp);

        foreach (var file in orphanedFiles)
            _files.Delete(file);

        await _audit.RecordAsync(adminId, "user.delete", "User", userId, description);
    }

    // ---- Content ----

    private sealed class ContentRow
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public Guid OwnerId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public async Task<PagedResult<GetAdminContentDto>> SearchContentAsync(string type, string? term, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var text = term?.Trim() ?? string.Empty;
        var kind = NormalizeType(type);

        IQueryable<ContentRow> rows = kind switch
        {
            "post" => _context.Posts.AsNoTracking().Where(p => text == "" || (p.Text != null && p.Text.Contains(text)))
                .Select(p => new ContentRow { Id = p.Id, Title = p.Text ?? "", OwnerId = p.AuthorId, CreatedAt = p.CreatedAt }),
            "comment" => _context.Comments.AsNoTracking().Where(c => text == "" || c.Text.Contains(text))
                .Select(c => new ContentRow { Id = c.Id, Title = c.Text, OwnerId = c.AuthorId, CreatedAt = c.CreatedAt }),
            "listing" => _context.MarketplaceListings.AsNoTracking().Where(l => text == "" || l.Title.Contains(text))
                .Select(l => new ContentRow { Id = l.Id, Title = l.Title, OwnerId = l.SellerId, CreatedAt = l.CreatedAt }),
            "event" => _context.Events.AsNoTracking().Where(e => text == "" || e.Title.Contains(text))
                .Select(e => new ContentRow { Id = e.Id, Title = e.Title, OwnerId = e.CreatedByUserId, CreatedAt = e.CreatedAt }),
            _ => _context.Groups.AsNoTracking().Where(g => text == "" || g.Name.Contains(text))
                .Select(g => new ContentRow { Id = g.Id, Title = g.Name, OwnerId = g.CreatedByUserId, CreatedAt = g.CreatedAt })
        };

        var total = await rows.CountAsync();
        var page_ = await rows.OrderByDescending(r => r.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var ownerIds = page_.Select(r => r.OwnerId).Distinct().ToList();
        var names = await _context.Users.Where(u => ownerIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName);

        var items = page_.Select(r => new GetAdminContentDto
        {
            Type = kind,
            Id = r.Id,
            Title = Shorten(r.Title, 140),
            OwnerId = r.OwnerId,
            OwnerName = names.GetValueOrDefault(r.OwnerId, string.Empty),
            CreatedAt = r.CreatedAt
        }).ToList();

        return new PagedResult<GetAdminContentDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task RemoveContentAsync(Guid adminId, string type, Guid id, string? note)
    {
        var kind = NormalizeType(type);
        var (title, ownerId) = await DescribeContentAsync(kind, id);

        switch (kind)
        {
            case "post": await _posts.DeleteAsync(adminId, id, asAdmin: true); break;
            case "comment": await _comments.DeleteAsync(adminId, id, asAdmin: true); break;
            case "listing": await _listings.DeleteAsync(adminId, id, asAdmin: true); break;
            case "event": await _events.DeleteAsync(adminId, id, asAdmin: true); break;
            default: await _groups.DeleteAsync(adminId, id, asAdmin: true); break;
        }

        var ownerName = await _context.Users.Where(u => u.Id == ownerId).Select(u => u.FullName).FirstOrDefaultAsync();
        var details = $"{ownerName}: “{Shorten(title, 120)}”" + (string.IsNullOrWhiteSpace(note) ? "" : $" — {note.Trim()}");
        await _audit.RecordAsync(adminId, $"{kind}.remove", char.ToUpperInvariant(kind[0]) + kind[1..], id, details);
    }

    // What the thing is called and whose it is, read before it is deleted (for the log).
    private async Task<(string Title, Guid OwnerId)> DescribeContentAsync(string kind, Guid id)
    {
        switch (kind)
        {
            case "post":
                var post = await _context.Posts.AsNoTracking().Where(p => p.Id == id).Select(p => new { p.Text, p.AuthorId }).FirstOrDefaultAsync()
                    ?? throw new NotFoundException(Loc.T("post.notFound"));
                return (post.Text ?? string.Empty, post.AuthorId);
            case "comment":
                var comment = await _context.Comments.AsNoTracking().Where(c => c.Id == id).Select(c => new { c.Text, c.AuthorId }).FirstOrDefaultAsync()
                    ?? throw new NotFoundException(Loc.T("comment.notFound"));
                return (comment.Text, comment.AuthorId);
            case "listing":
                var listing = await _context.MarketplaceListings.AsNoTracking().Where(l => l.Id == id).Select(l => new { l.Title, l.SellerId }).FirstOrDefaultAsync()
                    ?? throw new NotFoundException(Loc.T("listing.notFound"));
                return (listing.Title, listing.SellerId);
            case "event":
                var ev = await _context.Events.AsNoTracking().Where(e => e.Id == id).Select(e => new { e.Title, e.CreatedByUserId }).FirstOrDefaultAsync()
                    ?? throw new NotFoundException(Loc.T("event.notFound"));
                return (ev.Title, ev.CreatedByUserId);
            default:
                var group = await _context.Groups.AsNoTracking().Where(g => g.Id == id).Select(g => new { g.Name, g.CreatedByUserId }).FirstOrDefaultAsync()
                    ?? throw new NotFoundException(Loc.T("group.notFound"));
                return (group.Name, group.CreatedByUserId);
        }
    }

    // ---- Helpers ----

    private static string NormalizeType(string type)
    {
        var kind = (type ?? string.Empty).Trim().ToLowerInvariant();
        return kind is "post" or "comment" or "listing" or "event" or "group"
            ? kind
            : throw new BadRequestException(Loc.T("admin.typeInvalid"));
    }

    // The account an admin acts on: it has to exist, and it can be neither the admin's own nor another admin's.
    private async Task<ApplicationUser> FindActableUserAsync(Guid adminId, Guid userId)
    {
        if (userId == adminId)
            throw new BadRequestException(Loc.T("admin.cannotActOnSelf"));

        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException(Loc.T("user.notFound"));

        if (await _userManager.IsInRoleAsync(user, AppRoles.Admin))
            throw new ForbiddenException(Loc.T("admin.cannotActOnAdmin"));

        return user;
    }

    private static string Describe(ApplicationUser user, string? extra = null)
    {
        var text = $"{user.FullName} (@{user.UserName})";
        return string.IsNullOrWhiteSpace(extra) ? text : $"{text} — {extra}";
    }

    private static string Shorten(string text, int max)
    {
        text = text.Trim();
        return text.Length <= max ? text : text[..(max - 1)] + "…";
    }
}
