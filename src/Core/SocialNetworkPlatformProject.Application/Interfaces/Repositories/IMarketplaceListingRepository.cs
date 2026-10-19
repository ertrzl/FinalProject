using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IMarketplaceListingRepository : IRepository<MarketplaceListing>
{
    // "Benzer ilanlar": up to `take` listings that are on sale (Active, not expired) in the same category as the given
    // one, the closest in price first. Leaves out the listing itself, every listing of the given seller, the viewer's
    // own listings and the sellers in `hiddenSellerIds` (people blocked either way). Photos included.
    Task<List<MarketplaceListing>> GetSimilarAsync(MarketplaceListing listing, Guid viewerId,
        IReadOnlyCollection<Guid> hiddenSellerIds, DateTime now, int take);

    // Is one of the seller's listings featured ("Öne Çıkan") at the given moment?
    Task<bool> HasFeaturedAsync(Guid sellerId, DateTime now);
}
