using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class MarketplaceListingRepository : Repository<MarketplaceListing>, IMarketplaceListingRepository
{
    public MarketplaceListingRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<List<MarketplaceListing>> GetSimilarAsync(MarketplaceListing listing, Guid viewerId,
        IReadOnlyCollection<Guid> hiddenSellerIds, DateTime now, int take)
    {
        var price = listing.Price;

        return await _dbSet.AsNoTracking()
            .Include(l => l.Images)
            .Where(l => l.Status == ListingStatus.Active && l.ExpiresAt > now
                && l.Category == listing.Category
                && l.Currency == listing.Currency
                && l.Id != listing.Id
                && l.SellerId != listing.SellerId
                && l.SellerId != viewerId
                && !hiddenSellerIds.Contains(l.SellerId))
            .OrderBy(l => l.Price > price ? l.Price - price : price - l.Price)
            .ThenByDescending(l => l.CreatedAt)
            .ThenBy(l => l.Id)
            .Take(take)
            .ToListAsync();
    }

    public async Task<bool> HasFeaturedAsync(Guid sellerId, DateTime now)
    {
        return await _dbSet.AnyAsync(l => l.SellerId == sellerId && l.FeaturedUntil != null && l.FeaturedUntil > now);
    }
}
