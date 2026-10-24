using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IReportRepository : IRepository<Report>
{
    Task<ReportStatus?> GetStatusOfEarlierAsync(Guid reporterId, ReportTargetType targetType, Guid targetId);

    Task<(List<Report> Items, int TotalCount)> GetPageAsync(ReportStatus? status, int page, int pageSize);

    Task<List<Report>> GetPendingAboutAsync(ReportTargetType targetType, Guid targetId);

    Task<Dictionary<(ReportTargetType TargetType, Guid TargetId), int>> CountByTargetsAsync(IReadOnlyCollection<Guid> targetIds);

    Task<Dictionary<Guid, int>> CountByOwnersAsync(IReadOnlyCollection<Guid> ownerIds);
}
