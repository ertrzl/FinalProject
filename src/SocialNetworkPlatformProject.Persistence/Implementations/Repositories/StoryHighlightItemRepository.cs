using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class StoryHighlightItemRepository : Repository<StoryHighlightItem>, IStoryHighlightItemRepository
{
    public StoryHighlightItemRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Guid?> FindOwnerByMediaUrlAsync(string url)
    {
        return await _dbSet.AsNoTracking()
            .Where(i => i.MediaUrl == url)
            .Select(i => (Guid?)i.Highlight!.UserId)
            .FirstOrDefaultAsync();
    }
}
