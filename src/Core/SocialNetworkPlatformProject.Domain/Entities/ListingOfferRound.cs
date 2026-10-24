using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class ListingOfferRound : BaseEntity
{
    public Guid OfferId { get; set; }
    public ListingOffer? Offer { get; set; }

    public Guid ProposerId { get; set; }
    public decimal Price { get; set; }
}
