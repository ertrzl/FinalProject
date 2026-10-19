using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IListingBoostRepository : IRepository<ListingBoost>
{
    // When the seller last used "Öne Çıkar"; null if they never did.
    Task<DateTime?> GetLastBoostAtAsync(Guid sellerId);
}
