using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SocialNetworkPlatformProject.Application.DTOs.Admin;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;
using SocialNetworkPlatformProject.Persistence.Contexts;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;
    private readonly IPostAccessService _access;
    private readonly IAdminService _admin;
    private readonly IAuditService _audit;
    private readonly ILogger<ReportService> _logger;

    public ReportService(ApplicationDbContext context, IPostAccessService access, IAdminService admin, IAuditService audit, ILogger<ReportService> logger)
    {
        _logger = logger;
        _context = context;
        _access = access;
        _admin = admin;
        _audit = audit;
    }

    public async Task CreateAsync(Guid reporterId, PostReportDto dto)
    {
        var (ownerId, summary) = await FindTargetAsync(reporterId, dto.TargetType, dto.TargetId);

        if (ownerId == reporterId)
            throw new BadRequestException(Loc.T("report.own"));

        // One report per person and thing, for good: after the admins looked at it, reporting it again would only pile up
        // reports (the count next to a person is a sign the admins read) without telling them anything new.
        var earlier = await _context.Reports.AsNoTracking()
            .Where(r => r.ReporterId == reporterId && r.TargetType == dto.TargetType && r.TargetId == dto.TargetId)
            .Select(r => (ReportStatus?)r.Status)
            .FirstOrDefaultAsync();
        if (earlier == ReportStatus.Pending)
            throw new ConflictException(Loc.T("report.alreadyReported"));
        if (earlier != null)
            throw new ConflictException(Loc.T("report.alreadyReviewed"));

        _context.Reports.Add(new Report
        {
            ReporterId = reporterId,
            TargetType = dto.TargetType,
            TargetId = dto.TargetId,
            TargetOwnerId = ownerId,
            TargetSummary = summary,
            Reason = dto.Reason,
            Details = string.IsNullOrWhiteSpace(dto.Details) ? null : dto.Details.Trim()
        });
        await _context.SaveChangesAsync();
        _logger.LogInformation("Report: {TargetType:l} {TargetId:l} reported for {Reason:l}", dto.TargetType.ToString(), dto.TargetId.ToString(), dto.Reason.ToString());
    }

    // The person behind the reported thing and the start of it, as it is now. A thing the reporter could not see is
    // answered like one that does not exist, so reporting cannot be used to find out whether something is there.
    private async Task<(Guid OwnerId, string Summary)> FindTargetAsync(Guid reporterId, ReportTargetType type, Guid id)
    {
        switch (type)
        {
            case ReportTargetType.Post:
            {
                var post = await _context.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
                if (post == null || !await _access.CanViewAsync(post, reporterId))
                    throw new NotFoundException(Loc.T("report.targetNotFound"));
                return (post.AuthorId, Shorten(post.Text));
            }
            case ReportTargetType.Comment:
            {
                var comment = await _context.Comments.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
                var post = comment == null ? null : await _context.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == comment.PostId);
                if (comment == null || post == null || !await _access.CanViewAsync(post, reporterId))
                    throw new NotFoundException(Loc.T("report.targetNotFound"));
                return (comment.AuthorId, Shorten(comment.Text));
            }
            case ReportTargetType.User:
            {
                var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id)
                    ?? throw new NotFoundException(Loc.T("report.targetNotFound"));
                return (user.Id, $"{user.FullName} (@{user.UserName})");
            }
            default:
            {
                var listing = await _context.MarketplaceListings.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id)
                    ?? throw new NotFoundException(Loc.T("report.targetNotFound"));
                return (listing.SellerId, listing.Title);
            }
        }
    }

    public async Task<PagedResult<GetReportDto>> GetPageAsync(ReportStatus? status, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.Reports.AsNoTracking().AsQueryable();
        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        var total = await query.CountAsync();
        var reports = await query
            .OrderBy(r => r.Status == ReportStatus.Pending ? 0 : 1)
            .ThenByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<GetReportDto> { Items = await ToDtosAsync(reports), Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<GetReportDto> ResolveAsync(Guid adminId, Guid reportId, ResolveReportDto dto)
    {
        var report = await _context.Reports.FirstOrDefaultAsync(r => r.Id == reportId)
            ?? throw new NotFoundException(Loc.T("report.notFound"));

        if (report.Status != ReportStatus.Pending)
            throw new ConflictException(Loc.T("report.alreadyClosed"));

        var note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim();

        if (!dto.Dismiss)
        {
            // The account first: if it cannot be suspended (an admin's), nothing has been touched yet.
            if (dto.SuspendOwner)
                await _admin.SuspendUserAsync(adminId, report.TargetOwnerId, note ?? report.TargetSummary);

            if (dto.RemoveContent && report.TargetType != ReportTargetType.User)
            {
                try
                {
                    await _admin.RemoveContentAsync(adminId, report.TargetType.ToString(), report.TargetId, note);
                }
                catch (NotFoundException)
                {
                    // Already gone (the person deleted it, or another report was resolved first): nothing to remove.
                }
            }
        }

        var now = DateTime.UtcNow;
        var newStatus = dto.Dismiss ? ReportStatus.Dismissed : ReportStatus.Resolved;

        // Everyone who reported the same thing gets the same answer.
        var sameTarget = await _context.Reports
            .Where(r => r.TargetType == report.TargetType && r.TargetId == report.TargetId && r.Status == ReportStatus.Pending)
            .ToListAsync();

        foreach (var item in sameTarget)
        {
            item.Status = newStatus;
            item.ResolvedById = adminId;
            item.ResolvedAt = now;
            item.AdminNote = note;
        }
        await _context.SaveChangesAsync();

        // What the log says is written in the language of the admin who did it (the texts are in the language files).
        var summary = new List<string> { string.IsNullOrEmpty(report.TargetSummary) ? Loc.T("report.noText") : report.TargetSummary };
        if (!dto.Dismiss && dto.RemoveContent) summary.Add(Loc.T("admin.audit.contentRemoved"));
        if (!dto.Dismiss && dto.SuspendOwner) summary.Add(Loc.T("admin.audit.ownerSuspended"));
        if (note != null) summary.Add(note);
        await _audit.RecordAsync(adminId, dto.Dismiss ? "report.dismiss" : "report.resolve", report.TargetType.ToString(), report.TargetId, string.Join(" — ", summary));

        return (await ToDtosAsync(new[] { report }))[0];
    }

    // ---- Mapping ----

    private async Task<List<GetReportDto>> ToDtosAsync(IReadOnlyList<Report> reports)
    {
        var userIds = reports.SelectMany(r => new[] { r.ReporterId, r.TargetOwnerId, r.ResolvedById ?? Guid.Empty }).Distinct().ToList();
        var users = await _context.Users.AsNoTracking().Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName, u.IsSuspended }).ToDictionaryAsync(u => u.Id);

        var targetIds = reports.Select(r => r.TargetId).Distinct().ToList();
        var existing = new HashSet<(ReportTargetType, Guid)>();
        foreach (var id in await _context.Posts.Where(p => targetIds.Contains(p.Id)).Select(p => p.Id).ToListAsync()) existing.Add((ReportTargetType.Post, id));
        foreach (var id in await _context.Comments.Where(c => targetIds.Contains(c.Id)).Select(c => c.Id).ToListAsync()) existing.Add((ReportTargetType.Comment, id));
        foreach (var id in await _context.Users.Where(u => targetIds.Contains(u.Id)).Select(u => u.Id).ToListAsync()) existing.Add((ReportTargetType.User, id));
        foreach (var id in await _context.MarketplaceListings.Where(l => targetIds.Contains(l.Id)).Select(l => l.Id).ToListAsync()) existing.Add((ReportTargetType.Listing, id));

        var counts = (await _context.Reports.Where(r => targetIds.Contains(r.TargetId))
                .GroupBy(r => new { r.TargetType, r.TargetId }).Select(g => new { g.Key.TargetType, g.Key.TargetId, Count = g.Count() })
                .ToListAsync())
            .ToDictionary(x => (x.TargetType, x.TargetId), x => x.Count);

        string NameOf(Guid id) => users.TryGetValue(id, out var u) ? u.FullName : string.Empty;

        return reports.Select(r => new GetReportDto
        {
            Id = r.Id,
            CreatedAt = r.CreatedAt,
            Status = r.Status,
            Reason = r.Reason,
            Details = r.Details,
            ReporterId = r.ReporterId,
            ReporterName = NameOf(r.ReporterId),
            TargetType = r.TargetType,
            TargetId = r.TargetId,
            TargetSummary = r.TargetSummary,
            TargetOwnerId = r.TargetOwnerId,
            TargetOwnerName = NameOf(r.TargetOwnerId),
            TargetOwnerSuspended = users.TryGetValue(r.TargetOwnerId, out var owner) && owner.IsSuspended,
            TargetExists = existing.Contains((r.TargetType, r.TargetId)),
            ReportsAboutTarget = counts.GetValueOrDefault((r.TargetType, r.TargetId), 1),
            ResolvedByName = r.ResolvedById.HasValue ? NameOf(r.ResolvedById.Value) : null,
            ResolvedAt = r.ResolvedAt,
            AdminNote = r.AdminNote
        }).ToList();
    }

    // The start of a text as it is now; empty when there is none (a post with only a photo or a poll: the pages say so).
    private static string Shorten(string? text)
    {
        text = text?.Trim();
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text.Length <= 200 ? text : text[..199] + "…";
    }
}
