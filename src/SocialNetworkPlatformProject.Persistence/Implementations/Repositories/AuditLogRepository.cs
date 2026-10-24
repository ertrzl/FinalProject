using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class AuditLogRepository : Repository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<(List<AuditLog> Items, int TotalCount)> SearchAsync(string? action, string? term, int page, int pageSize)
    {
        var query = _dbSet.AsNoTracking().AsQueryable();

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
            .ToListAsync();

        return (items, total);
    }

    public async Task<int> DeleteOlderThanAsync(DateTime limitUtc)
    {
        return await _dbSet.Where(a => a.CreatedAt < limitUtc).ExecuteDeleteAsync();
    }
}
