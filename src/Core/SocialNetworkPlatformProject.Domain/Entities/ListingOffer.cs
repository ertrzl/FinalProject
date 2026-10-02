using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

// One negotiation thread: a buyer's offer on a listing plus every counter-offer after it (see Rounds).
// Strictly turn-based: whoever did NOT make the last proposal is the one who must answer it.
public class ListingOffer : BaseEntity
{
    public Guid ListingId { get; set; }
    public MarketplaceListing? Listing { get; set; }

    public Guid BuyerId { get; set; }
    public Guid SellerId { get; set; } // copied from the listing so inbox queries need no join

    public OfferStatus Status { get; set; } = OfferStatus.Open;
    public decimal CurrentPrice { get; set; }
    public Guid LastProposerId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ListingOfferRound> Rounds { get; set; } = new List<ListingOfferRound>();
}
