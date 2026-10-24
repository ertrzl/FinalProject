using SocialNetworkPlatformProject.Application.DTOs.Admin;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IAuditService
{
    Task RecordAsync(Guid actorId, string action, string? targetType = null, Guid? targetId = null, string? details = null);

    Task<PagedResult<GetAuditLogDto>> GetPageAsync(string? action, string? term, int page, int pageSize);
}

public interface IAdminService
{
    Task<GetAdminStatsDto> GetStatsAsync();

    Task<PagedResult<GetAdminUserDto>> SearchUsersAsync(string? term, bool? suspended, int page, int pageSize);

    Task SuspendUserAsync(Guid adminId, Guid userId, string? reason);

    Task UnsuspendUserAsync(Guid adminId, Guid userId);

    Task DeleteUserAsync(Guid adminId, Guid userId);

    Task<PagedResult<GetAdminContentDto>> SearchContentAsync(string type, string? term, int page, int pageSize);

    Task RemoveContentAsync(Guid adminId, string type, Guid id, string? note);
}

public interface IReportService
{
    Task CreateAsync(Guid reporterId, PostReportDto dto);

    Task<PagedResult<GetReportDto>> GetPageAsync(ReportStatus? status, int page, int pageSize);

    Task<GetReportDto> ResolveAsync(Guid adminId, Guid reportId, ResolveReportDto dto);
}
