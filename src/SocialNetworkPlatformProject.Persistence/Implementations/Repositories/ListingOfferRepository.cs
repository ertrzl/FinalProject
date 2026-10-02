using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class ListingOfferRepository : Repository<ListingOffer>, IListingOfferRepository
{
    public ListingOfferRepository(ApplicationDbContext context) : base(context)
    {
    }
}
