using Microsoft.Extensions.Logging;
using SocialNetworkPlatformProject.Application.DTOs.Admin;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _auditLogs;
    private readonly IUserRepository _users;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IAuditLogRepository auditLogs, IUserRepository users, ILogger<AuditService> logger)
    {
        _auditLogs = auditLogs;
        _users = users;
        _logger = logger;
    }

    public async Task RecordAsync(Guid actorId, string action, string? targetType = null, Guid? targetId = null, string? details = null)
    {
        var actorName = (await _users.GetSummaryAsync(actorId))?.FullName;

        await _auditLogs.AddAsync(new AuditLog
        {
            ActorId = actorId,
            ActorName = actorName ?? string.Empty,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Details = Shorten(details, 500)
        });
        await _auditLogs.SaveChangesAsync();

        _logger.LogInformation("Admin action {Action} by {ActorName} ({ActorId:l}) on {TargetType} {TargetId:l}", action, actorName, actorId.ToString(), targetType, targetId?.ToString());
    }

    public async Task<PagedResult<GetAuditLogDto>> GetPageAsync(string? action, string? term, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (lines, total) = await _auditLogs.SearchAsync(action, term, page, pageSize);

        var items = lines.Select(a => new GetAuditLogDto
        {
            Id = a.Id,
            CreatedAt = a.CreatedAt,
            ActorId = a.ActorId,
            ActorName = a.ActorName,
            Action = a.Action,
            TargetType = a.TargetType,
            TargetId = a.TargetId,
            Details = a.Details
        }).ToList();

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
