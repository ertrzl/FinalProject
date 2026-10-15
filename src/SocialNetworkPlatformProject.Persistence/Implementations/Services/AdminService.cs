using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.DTOs.Admin;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Enums;
using SocialNetworkPlatformProject.Persistence.Identity;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class AdminService : IAdminService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserRepository _users;
    private readonly IPostRepository _postRows;
    private readonly ICommentRepository _commentRows;
    private readonly IMarketplaceListingRepository _listingRows;
    private readonly IEventRepository _eventRows;
    private readonly IGroupRepository _groupRows;
    private readonly IReportRepository _reports;
    private readonly IAccessTokenRevoker _accessTokens;
    private readonly IFileStorageService _files;
    private readonly SessionIssuer _sessions;
    private readonly IPostService _posts;
    private readonly ICommentService _comments;
    private readonly IMarketplaceService _listings;
    private readonly IEventService _events;
    private readonly IGroupService _groups;
    private readonly IAuditService _audit;
    private readonly INotificationService _notifications;

    public AdminService(
        UserManager<ApplicationUser> userManager,
        IUserRepository users,
        IPostRepository postRows,
        ICommentRepository commentRows,
        IMarketplaceListingRepository listingRows,
        IEventRepository eventRows,
        IGroupRepository groupRows,
        IReportRepository reports,
        IAccessTokenRevoker accessTokens,
        IFileStorageService files,
        SessionIssuer sessions,
        IPostService posts,
        ICommentService comments,
        IMarketplaceService listings,
        IEventService events,
        IGroupService groups,
        IAuditService audit,
        INotificationService notifications)
    {
        _userManager = userManager;
        _users = users;
        _postRows = postRows;
        _commentRows = commentRows;
        _listingRows = listingRows;
        _eventRows = eventRows;
        _groupRows = groupRows;
        _reports = reports;
        _accessTokens = accessTokens;
        _files = files;
        _sessions = sessions;
        _posts = posts;
        _comments = comments;
        _listings = listings;
        _events = events;
        _groups = groups;
        _audit = audit;
        _notifications = notifications;
    }

    public async Task<GetAdminStatsDto> GetStatsAsync()
    {
        var now = DateTime.UtcNow;
        var weekAgo = now.AddDays(-7);

        return new GetAdminStatsDto
        {
            Users = await _users.CountAsync(),
            SuspendedUsers = await _users.CountAsync(suspended: true),
            NewUsersThisWeek = await _users.CountAsync(createdSince: weekAgo),
            Posts = await _postRows.GetAll().CountAsync(),
            PostsToday = await _postRows.GetAll(p => p.CreatedAt >= now.Date).CountAsync(),
            Listings = await _listingRows.GetAll().CountAsync(),
            Events = await _eventRows.GetAll().CountAsync(),
            Groups = await _groupRows.GetAll().CountAsync(),
            PendingReports = await _reports.GetAll(r => r.Status == ReportStatus.Pending).CountAsync(),
            ResolvedReports = await _reports.GetAll(r => r.Status != ReportStatus.Pending).CountAsync()
        };
    }

    public async Task<PagedResult<GetAdminUserDto>> SearchUsersAsync(string? term, bool? suspended, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var (found, total) = await _users.SearchForAdminAsync(term, suspended, page, pageSize);

        var ids = found.Select(u => u.Id).ToList();

        var postCounts = await _postRows.GetAll(p => ids.Contains(p.AuthorId), asNoTracking: true)
            .GroupBy(p => p.AuthorId).Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count);
        var reportCounts = await _reports.CountByOwnersAsync(ids);

        var items = found.Select(u => new GetAdminUserDto
        {
            Id = u.Id,
            FullName = u.FullName,
            UserName = u.UserName,
            Email = u.Email,
            AvatarUrl = u.AvatarUrl,
            CreatedAt = u.CreatedAt,
            LastSeenAt = u.LastSeenAt,
            IsAdmin = u.IsAdmin,
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
            "post" => _postRows.GetAll(p => text == "" || (p.Text != null && p.Text.Contains(text)), asNoTracking: true)
                .Select(p => new ContentRow { Id = p.Id, Title = p.Text ?? "", OwnerId = p.AuthorId, CreatedAt = p.CreatedAt }),
            "comment" => _commentRows.GetAll(c => text == "" || c.Text.Contains(text), asNoTracking: true)
                .Select(c => new ContentRow { Id = c.Id, Title = c.Text, OwnerId = c.AuthorId, CreatedAt = c.CreatedAt }),
            "listing" => _listingRows.GetAll(l => text == "" || l.Title.Contains(text), asNoTracking: true)
                .Select(l => new ContentRow { Id = l.Id, Title = l.Title, OwnerId = l.SellerId, CreatedAt = l.CreatedAt }),
            "event" => _eventRows.GetAll(e => text == "" || e.Title.Contains(text), asNoTracking: true)
                .Select(e => new ContentRow { Id = e.Id, Title = e.Title, OwnerId = e.CreatedByUserId, CreatedAt = e.CreatedAt }),
            _ => _groupRows.GetAll(g => text == "" || g.Name.Contains(text), asNoTracking: true)
                .Select(g => new ContentRow { Id = g.Id, Title = g.Name, OwnerId = g.CreatedByUserId, CreatedAt = g.CreatedAt })
        };

        var total = await rows.CountAsync();
        var page_ = await rows.OrderByDescending(r => r.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var ownerIds = page_.Select(r => r.OwnerId).Distinct().ToList();
        var owners = await _users.GetSummariesAsync(ownerIds);

        var items = page_.Select(r => new GetAdminContentDto
        {
            Type = kind,
            Id = r.Id,
            Title = Shorten(r.Title, 140),
            OwnerId = r.OwnerId,
            OwnerName = owners.TryGetValue(r.OwnerId, out var owner) ? owner.FullName : string.Empty,
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

        // The owner is told that it was removed (not by whom). Nothing is sent when the admin removed their own thing.
        await _notifications.CreateAsync(ownerId, adminId, NotificationType.ContentRemoved, subject: string.IsNullOrWhiteSpace(title) ? null : Shorten(title, 150));

        var ownerName = (await _users.GetSummaryAsync(ownerId))?.FullName;
        var details = $"{ownerName}: “{Shorten(title, 120)}”" + (string.IsNullOrWhiteSpace(note) ? "" : $" — {note.Trim()}");
        await _audit.RecordAsync(adminId, $"{kind}.remove", char.ToUpperInvariant(kind[0]) + kind[1..], id, details);
    }

    // What the thing is called and whose it is, read before it is deleted (for the log).
    private async Task<(string Title, Guid OwnerId)> DescribeContentAsync(string kind, Guid id)
    {
        switch (kind)
        {
            case "post":
                var post = await _postRows.GetAll(p => p.Id == id, asNoTracking: true).Select(p => new { p.Text, p.AuthorId }).FirstOrDefaultAsync()
                    ?? throw new NotFoundException(Loc.T("post.notFound"));
                return (post.Text ?? string.Empty, post.AuthorId);
            case "comment":
                var comment = await _commentRows.GetAll(c => c.Id == id, asNoTracking: true).Select(c => new { c.Text, c.AuthorId }).FirstOrDefaultAsync()
                    ?? throw new NotFoundException(Loc.T("comment.notFound"));
                return (comment.Text, comment.AuthorId);
            case "listing":
                var listing = await _listingRows.GetAll(l => l.Id == id, asNoTracking: true).Select(l => new { l.Title, l.SellerId }).FirstOrDefaultAsync()
                    ?? throw new NotFoundException(Loc.T("listing.notFound"));
                return (listing.Title, listing.SellerId);
            case "event":
                var ev = await _eventRows.GetAll(e => e.Id == id, asNoTracking: true).Select(e => new { e.Title, e.CreatedByUserId }).FirstOrDefaultAsync()
                    ?? throw new NotFoundException(Loc.T("event.notFound"));
                return (ev.Title, ev.CreatedByUserId);
            default:
                var group = await _groupRows.GetAll(g => g.Id == id, asNoTracking: true).Select(g => new { g.Name, g.CreatedByUserId }).FirstOrDefaultAsync()
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
