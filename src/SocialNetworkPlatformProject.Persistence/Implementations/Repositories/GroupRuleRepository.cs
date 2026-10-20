using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class GroupRuleRepository : Repository<GroupRule>, IGroupRuleRepository
{
    public GroupRuleRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<GroupRule>> GetForGroupAsync(Guid groupId)
    {
        return await _dbSet.AsNoTracking()
            .Where(r => r.GroupId == groupId)
            .OrderBy(r => r.SortOrder)
            .ToListAsync();
    }

    public async Task<Dictionary<Guid, int>> CountByGroupAsync(IReadOnlyCollection<Guid> groupIds)
    {
        if (groupIds.Count == 0)
            return new Dictionary<Guid, int>();

        return await _dbSet.AsNoTracking()
            .Where(r => groupIds.Contains(r.GroupId))
            .GroupBy(r => r.GroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count);
    }
}
