using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class GroupMemberRepository : Repository<GroupMember>, IGroupMemberRepository
{
    public GroupMemberRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<(List<GroupMember> Items, int TotalCount)> SearchAsync(
        Guid groupId, Guid ownerId, string? term, GroupMemberRole? role, bool ownerOnly, int page, int pageSize)
    {
        var users = _context.Users;
        var query = _dbSet.AsNoTracking().Where(m => m.GroupId == groupId);

        if (ownerOnly)
            query = query.Where(m => m.UserId == ownerId);
        else if (role.HasValue)
            query = query.Where(m => m.Role == role.Value);

        // The name lives on the account, so the members are matched against the users table.
        if (!string.IsNullOrWhiteSpace(term))
        {
            var t = term.Trim();
            query = query.Where(m => users.Any(u => u.Id == m.UserId
                && (u.FullName.Contains(t) || u.UserName!.Contains(t))));
        }

        // A member whose account is gone is not listed (and not counted).
        query = query.Where(m => users.Any(u => u.Id == m.UserId));

        var total = await query.CountAsync();

        var items = await query
            // The owner, then Admin, Moderator, Member (not by the enum's number: Moderator was added after Admin), then by name.
            .OrderBy(m => m.UserId == ownerId ? 0 : m.Role == GroupMemberRole.Admin ? 1 : m.Role == GroupMemberRole.Moderator ? 2 : 3)
            .ThenBy(m => users.Where(u => u.Id == m.UserId).Select(u => u.FullName).FirstOrDefault())
            .ThenBy(m => m.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }
}
