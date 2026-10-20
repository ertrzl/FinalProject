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

    public async Task<bool> IsActivityMutedAsync(Guid userId, Guid? postId, Guid? eventId)
    {
        Guid? groupId = null;
        if (postId.HasValue)
            groupId = await _context.Set<Post>().Where(p => p.Id == postId.Value).Select(p => p.GroupId).FirstOrDefaultAsync();
        if (groupId == null && eventId.HasValue)
            groupId = await _context.Set<Event>().Where(e => e.Id == eventId.Value).Select(e => e.GroupId).FirstOrDefaultAsync();
        if (groupId == null)
            return false;

        return await _context.Set<GroupMember>().AnyAsync(m => m.GroupId == groupId.Value && m.UserId == userId && m.IsMuted);
    }
}
