using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IMarketplaceListingRepository : IRepository<MarketplaceListing>
{
    Task<List<MarketplaceListing>> GetSimilarAsync(MarketplaceListing listing, Guid viewerId,
        IReadOnlyCollection<Guid> hiddenSellerIds, DateTime now, int take);

    Task<bool> HasFeaturedAsync(Guid sellerId, DateTime now);
}
