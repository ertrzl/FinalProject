using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class GroupPinnedPostRepository : Repository<GroupPinnedPost>, IGroupPinnedPostRepository
{
    public GroupPinnedPostRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<Guid>> GetPinnedPostIdsAsync(Guid groupId)
    {
        return await _dbSet.AsNoTracking()
            .Where(p => p.GroupId == groupId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => p.PostId)
            .ToListAsync();
    }
}
