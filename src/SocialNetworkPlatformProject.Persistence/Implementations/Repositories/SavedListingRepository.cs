using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class SavedListingRepository : Repository<SavedListing>, ISavedListingRepository
{
    public SavedListingRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<Guid>> GetSaverIdsAsync(Guid listingId)
    {
        return await _dbSet.AsNoTracking()
            .Where(s => s.ListingId == listingId)
            .Select(s => s.UserId)
            .ToListAsync();
    }
}
