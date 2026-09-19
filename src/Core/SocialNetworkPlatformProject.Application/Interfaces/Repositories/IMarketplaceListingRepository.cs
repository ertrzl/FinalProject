using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IMarketplaceListingRepository : IRepository<MarketplaceListing>
{
    // Entity-specific query methods will be added here as the MarketplaceListing service needs them.
}
