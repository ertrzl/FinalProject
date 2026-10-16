using SocialNetworkPlatformProject.Application.DTOs.Admin;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// The record of what admins did. Every admin action writes a line here; the admin panel reads them.
public interface IAuditService
{
    Task RecordAsync(Guid actorId, string action, string? targetType = null, Guid? targetId = null, string? details = null);

    Task<PagedResult<GetAuditLogDto>> GetPageAsync(string? action, string? term, int page, int pageSize);
}

// What an admin can do to accounts and content. The controller only lets admins in (the Admin role); the rules about
// whom an admin may act on (not an admin, not themselves) are here.
public interface IAdminService
{
    Task<GetAdminStatsDto> GetStatsAsync();

    Task<PagedResult<GetAdminUserDto>> SearchUsersAsync(string? term, bool? suspended, int page, int pageSize);

    // Ends the account's sessions at once and blocks signing in until it is lifted.
    Task SuspendUserAsync(Guid adminId, Guid userId, string? reason);

    Task UnsuspendUserAsync(Guid adminId, Guid userId);

    // Deletes the account with everything it owns (the same cleanup as when a person deletes their own account).
    Task DeleteUserAsync(Guid adminId, Guid userId);

    // Posts, comments, listings, events and groups: find them (text search) ...
    Task<PagedResult<GetAdminContentDto>> SearchContentAsync(string type, string? term, int page, int pageSize);

    // ... and remove one. `type` is "post", "comment", "listing", "event" or "group".
    Task RemoveContentAsync(Guid adminId, string type, Guid id, string? note);
}

public interface IReportService
{
    Task CreateAsync(Guid reporterId, PostReportDto dto);

    Task<PagedResult<GetReportDto>> GetPageAsync(ReportStatus? status, int page, int pageSize);

    Task<GetReportDto> ResolveAsync(Guid adminId, Guid reportId, ResolveReportDto dto);
}
