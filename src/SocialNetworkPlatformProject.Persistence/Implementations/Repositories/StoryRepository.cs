using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class StoryRepository : Repository<Story>, IStoryRepository
{
    public StoryRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<Story>> GetExpiredAsync(DateTime nowUtc, int take)
    {
        return await _dbSet.AsNoTracking()
            .Where(s => s.ExpiresAt <= nowUtc)
            .OrderBy(s => s.ExpiresAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<int> DeleteByIdsAsync(IReadOnlyCollection<Guid> ids)
    {
        return await _dbSet.Where(s => ids.Contains(s.Id)).ExecuteDeleteAsync();
    }
}
