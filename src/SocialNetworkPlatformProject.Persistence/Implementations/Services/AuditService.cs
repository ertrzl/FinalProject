using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SocialNetworkPlatformProject.Application.DTOs.Admin;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Persistence.Contexts;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(ApplicationDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task RecordAsync(Guid actorId, string action, string? targetType = null, Guid? targetId = null, string? details = null)
    {
        var actorName = await _context.Users.Where(u => u.Id == actorId).Select(u => u.FullName).FirstOrDefaultAsync();

        _context.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId,
            ActorName = actorName ?? string.Empty,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Details = Shorten(details, 500)
        });
        await _context.SaveChangesAsync();

        // The same line goes to the log file, so what the admins did can be read there as well as in the panel.
        _logger.LogInformation("Admin action {Action} by {ActorName} ({ActorId:l}) on {TargetType} {TargetId:l}", action, actorName, actorId.ToString(), targetType, targetId?.ToString());
    }

    public async Task<PagedResult<GetAuditLogDto>> GetPageAsync(string? action, string? term, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        // "user" shows every "user.*" line (the panel's filter by kind), "user.suspend" only that one.
        if (!string.IsNullOrWhiteSpace(action))
        {
            var kind = action.Trim();
            query = kind.Contains('.') ? query.Where(a => a.Action == kind) : query.Where(a => a.Action.StartsWith(kind + "."));
        }

        if (!string.IsNullOrWhiteSpace(term))
        {
            var text = term.Trim();
            query = query.Where(a => a.ActorName.Contains(text) || (a.Details != null && a.Details.Contains(text)));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new GetAuditLogDto
            {
                Id = a.Id,
                CreatedAt = a.CreatedAt,
                ActorId = a.ActorId,
                ActorName = a.ActorName,
                Action = a.Action,
                TargetType = a.TargetType,
                TargetId = a.TargetId,
                Details = a.Details
            })
            .ToListAsync();

        return new PagedResult<GetAuditLogDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    private static string? Shorten(string? text, int max)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        text = text.Trim();
        return text.Length <= max ? text : text[..(max - 1)] + "…";
    }
}
