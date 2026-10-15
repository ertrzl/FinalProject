using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IReportRepository : IRepository<Report>
{
    // The status of the report this person already made about this thing; null when they never did.
    Task<ReportStatus?> GetStatusOfEarlierAsync(Guid reporterId, ReportTargetType targetType, Guid targetId);

    // Pending reports first, then newest first; `status` null = all of them.
    Task<(List<Report> Items, int TotalCount)> GetPageAsync(ReportStatus? status, int page, int pageSize);

    // Every report about this thing that nobody has answered yet (tracked, so the caller can close them).
    Task<List<Report>> GetPendingAboutAsync(ReportTargetType targetType, Guid targetId);

    // How many reports (of any status) there are about each thing.
    Task<Dictionary<(ReportTargetType TargetType, Guid TargetId), int>> CountByTargetsAsync(IReadOnlyCollection<Guid> targetIds);

    // How many reports (of any status) there are about the things each of these people own.
    Task<Dictionary<Guid, int>> CountByOwnersAsync(IReadOnlyCollection<Guid> ownerIds);
}
