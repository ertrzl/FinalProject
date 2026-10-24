using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SocialNetworkPlatformProject.Application.DTOs.Admin;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;
using SocialNetworkPlatformProject.Persistence.Extensions;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class ReportService : IReportService
{
    private readonly IReportRepository _reports;
    private readonly IPostRepository _posts;
    private readonly ICommentRepository _comments;
    private readonly IMarketplaceListingRepository _listings;
    private readonly IUserRepository _users;
    private readonly IPostAccessService _access;
    private readonly IAdminService _admin;
    private readonly IAuditService _audit;
    private readonly ILogger<ReportService> _logger;

    public ReportService(
        IReportRepository reports,
        IPostRepository posts,
        ICommentRepository comments,
        IMarketplaceListingRepository listings,
        IUserRepository users,
        IPostAccessService access,
        IAdminService admin,
        IAuditService audit,
        ILogger<ReportService> logger)
    {
        _reports = reports;
        _posts = posts;
        _comments = comments;
        _listings = listings;
        _users = users;
        _access = access;
        _admin = admin;
        _audit = audit;
        _logger = logger;
    }

    public async Task CreateAsync(Guid reporterId, PostReportDto dto)
    {
        var (ownerId, summary) = await FindTargetAsync(reporterId, dto.TargetType, dto.TargetId);

        if (ownerId == reporterId)
            throw new BadRequestException(Loc.T("report.own"));

        var earlier = await _reports.GetStatusOfEarlierAsync(reporterId, dto.TargetType, dto.TargetId);
        if (earlier == ReportStatus.Pending)
            throw new ConflictException(Loc.T("report.alreadyReported"));
        if (earlier != null)
            throw new ConflictException(Loc.T("report.alreadyReviewed"));

        await _reports.AddAsync(new Report
        {
            ReporterId = reporterId,
            TargetType = dto.TargetType,
            TargetId = dto.TargetId,
            TargetOwnerId = ownerId,
            TargetSummary = summary,
            Reason = dto.Reason,
            Details = string.IsNullOrWhiteSpace(dto.Details) ? null : dto.Details.Trim()
        });

        try
        {
            await _reports.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.IsUniqueViolation())
        {
            throw new ConflictException(Loc.T("report.alreadyReported"));
        }

        _logger.LogInformation("Report: {TargetType:l} {TargetId:l} reported for {Reason:l}", dto.TargetType.ToString(), dto.TargetId.ToString(), dto.Reason.ToString());
    }

    private async Task<(Guid OwnerId, string Summary)> FindTargetAsync(Guid reporterId, ReportTargetType type, Guid id)
    {
        switch (type)
        {
            case ReportTargetType.Post:
            {
                var post = await _posts.GetAll(p => p.Id == id, asNoTracking: true).FirstOrDefaultAsync();
                if (post == null || !await _access.CanViewAsync(post, reporterId))
                    throw new NotFoundException(Loc.T("report.targetNotFound"));
                return (post.AuthorId, Shorten(post.Text));
            }
            case ReportTargetType.Comment:
            {
                var comment = await _comments.GetAll(c => c.Id == id, asNoTracking: true).FirstOrDefaultAsync();
                var post = comment == null ? null : await _posts.GetAll(p => p.Id == comment.PostId, asNoTracking: true).FirstOrDefaultAsync();
                if (comment == null || post == null || !await _access.CanViewAsync(post, reporterId))
                    throw new NotFoundException(Loc.T("report.targetNotFound"));
                return (comment.AuthorId, Shorten(comment.Text));
            }
            case ReportTargetType.User:
            {
                var user = await _users.GetSummaryAsync(id)
                    ?? throw new NotFoundException(Loc.T("report.targetNotFound"));
                return (user.Id, $"{user.FullName} (@{user.UserName})");
            }
            default:
            {
                var listing = await _listings.GetAll(l => l.Id == id, asNoTracking: true).FirstOrDefaultAsync()
                    ?? throw new NotFoundException(Loc.T("report.targetNotFound"));
                return (listing.SellerId, listing.Title);
            }
        }
    }

    public async Task<PagedResult<GetReportDto>> GetPageAsync(ReportStatus? status, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var (reports, total) = await _reports.GetPageAsync(status, page, pageSize);

        return new PagedResult<GetReportDto> { Items = await ToDtosAsync(reports), Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<GetReportDto> ResolveAsync(Guid adminId, Guid reportId, ResolveReportDto dto)
    {
        var report = await _reports.GetByIdAsync(reportId)
            ?? throw new NotFoundException(Loc.T("report.notFound"));

        if (report.Status != ReportStatus.Pending)
            throw new ConflictException(Loc.T("report.alreadyClosed"));

        var note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim();

        await using (var transaction = await _reports.BeginTransactionAsync())
        {
            var now = DateTime.UtcNow;
            var newStatus = dto.Dismiss ? ReportStatus.Dismissed : ReportStatus.Resolved;

            var sameTarget = await _reports.GetPendingAboutAsync(report.TargetType, report.TargetId);

            foreach (var item in sameTarget)
            {
                item.Status = newStatus;
                item.ResolvedById = adminId;
                item.ResolvedAt = now;
                item.AdminNote = note;
            }
            await _reports.SaveChangesAsync();

            var summary = new List<string> { string.IsNullOrEmpty(report.TargetSummary) ? Loc.T("report.noText") : report.TargetSummary };
            if (!dto.Dismiss && dto.RemoveContent) summary.Add(Loc.T("admin.audit.contentRemoved"));
            if (!dto.Dismiss && dto.SuspendOwner) summary.Add(Loc.T("admin.audit.ownerSuspended"));
            if (note != null) summary.Add(note);
            await _audit.RecordAsync(adminId, dto.Dismiss ? "report.dismiss" : "report.resolve", report.TargetType.ToString(), report.TargetId, string.Join(" — ", summary));

            if (!dto.Dismiss)
            {
                if (dto.RemoveContent && report.TargetType != ReportTargetType.User)
                {
                    try
                    {
                        await _admin.RemoveContentAsync(adminId, report.TargetType.ToString(), report.TargetId, note);
                    }
                    catch (NotFoundException)
                    {
                    }
                }

                if (dto.SuspendOwner)
                    await _admin.SuspendUserAsync(adminId, report.TargetOwnerId, note ?? report.TargetSummary);
            }

            await transaction.CommitAsync();
        }

        return (await ToDtosAsync(new[] { report }))[0];
    }


    private async Task<List<GetReportDto>> ToDtosAsync(IReadOnlyList<Report> reports)
    {
        var userIds = reports.SelectMany(r => new[] { r.ReporterId, r.TargetOwnerId, r.ResolvedById ?? Guid.Empty }).Distinct().ToList();
        var users = await _users.GetSummariesAsync(userIds);

        var targetIds = reports.Select(r => r.TargetId).Distinct().ToList();
        var existing = new HashSet<(ReportTargetType, Guid)>();
        foreach (var id in await _posts.GetAll(p => targetIds.Contains(p.Id), asNoTracking: true).Select(p => p.Id).ToListAsync()) existing.Add((ReportTargetType.Post, id));
        foreach (var id in await _comments.GetAll(c => targetIds.Contains(c.Id), asNoTracking: true).Select(c => c.Id).ToListAsync()) existing.Add((ReportTargetType.Comment, id));
        foreach (var id in users.Keys.Where(targetIds.Contains)) existing.Add((ReportTargetType.User, id));
        foreach (var id in await _listings.GetAll(l => targetIds.Contains(l.Id), asNoTracking: true).Select(l => l.Id).ToListAsync()) existing.Add((ReportTargetType.Listing, id));

        var counts = await _reports.CountByTargetsAsync(targetIds);

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

    private static string Shorten(string? text)
    {
        text = text?.Trim();
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text.Length <= 200 ? text : text[..199] + "…";
    }
}
