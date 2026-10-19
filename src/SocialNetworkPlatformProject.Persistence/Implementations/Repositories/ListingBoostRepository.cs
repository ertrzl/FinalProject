using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class ListingBoostRepository : Repository<ListingBoost>, IListingBoostRepository
{
    public ListingBoostRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<DateTime?> GetLastBoostAtAsync(Guid sellerId)
    {
        return await _dbSet.AsNoTracking()
            .Where(b => b.SellerId == sellerId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => (DateTime?)b.CreatedAt)
            .FirstOrDefaultAsync();
    }
}
