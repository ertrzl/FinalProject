using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface ISavedListingRepository : IRepository<SavedListing>
{
    // Everybody who has the listing in their favourites (the people a price-drop alert goes to).
    Task<List<Guid>> GetSaverIdsAsync(Guid listingId);
}
