using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// A single price proposal inside a negotiation (the buyer's offer or either side's counter-offer).
public class ListingOfferRound : BaseEntity
{
    public Guid OfferId { get; set; }
    public ListingOffer? Offer { get; set; }

    public Guid ProposerId { get; set; }
    public decimal Price { get; set; }
}
