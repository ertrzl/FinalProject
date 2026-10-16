using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class GroupRepository : Repository<Group>, IGroupRepository
{
    public GroupRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Dictionary<Guid, GroupMembership>> GetMembershipsAsync(IReadOnlyCollection<Guid> groupIds, Guid viewerId)
    {
        var rows = await _context.Set<GroupMember>()
            .Where(m => groupIds.Contains(m.GroupId))
            .GroupBy(m => m.GroupId)
            .Select(g => new
            {
                GroupId = g.Key,
                Count = g.Count(),
                ViewerRole = g.Where(m => m.UserId == viewerId).Select(m => (GroupMemberRole?)m.Role).FirstOrDefault()
            })
            .ToListAsync();

        var found = rows.ToDictionary(r => r.GroupId, r => new GroupMembership(r.Count, r.ViewerRole));

        // A group nobody belongs to has no rows at all.
        return groupIds.Distinct().ToDictionary(id => id, id => found.GetValueOrDefault(id, new GroupMembership(0, null)));
    }
}
