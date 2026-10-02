using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Persistence.Contexts;
using SocialNetworkPlatformProject.Persistence.Implementations.Repositories.Generic;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Repositories;

public class ListingOfferRoundRepository : Repository<ListingOfferRound>, IListingOfferRoundRepository
{
    public ListingOfferRoundRepository(ApplicationDbContext context) : base(context)
    {
    }
}
