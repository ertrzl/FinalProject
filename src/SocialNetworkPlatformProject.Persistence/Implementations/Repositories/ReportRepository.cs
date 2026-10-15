using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class ReportRepository : Repository<Report>, IReportRepository
{
    public ReportRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<ReportStatus?> GetStatusOfEarlierAsync(Guid reporterId, ReportTargetType targetType, Guid targetId)
    {
        return await _dbSet.AsNoTracking()
            .Where(r => r.ReporterId == reporterId && r.TargetType == targetType && r.TargetId == targetId)
            .Select(r => (ReportStatus?)r.Status)
            .FirstOrDefaultAsync();
    }

    public async Task<(List<Report> Items, int TotalCount)> GetPageAsync(ReportStatus? status, int page, int pageSize)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();
        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(r => r.Status == ReportStatus.Pending ? 0 : 1)
            .ThenByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<List<Report>> GetPendingAboutAsync(ReportTargetType targetType, Guid targetId)
    {
        return await _dbSet
            .Where(r => r.TargetType == targetType && r.TargetId == targetId && r.Status == ReportStatus.Pending)
            .ToListAsync();
    }

    public async Task<Dictionary<(ReportTargetType TargetType, Guid TargetId), int>> CountByTargetsAsync(IReadOnlyCollection<Guid> targetIds)
    {
        var counts = await _dbSet.Where(r => targetIds.Contains(r.TargetId))
            .GroupBy(r => new { r.TargetType, r.TargetId })
            .Select(g => new { g.Key.TargetType, g.Key.TargetId, Count = g.Count() })
            .ToListAsync();

        return counts.ToDictionary(x => (x.TargetType, x.TargetId), x => x.Count);
    }

    public async Task<Dictionary<Guid, int>> CountByOwnersAsync(IReadOnlyCollection<Guid> ownerIds)
    {
        return await _dbSet.Where(r => ownerIds.Contains(r.TargetOwnerId))
            .GroupBy(r => r.TargetOwnerId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count);
    }
}
