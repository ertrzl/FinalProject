using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class ListingOffer : BaseEntity
{
    public Guid ListingId { get; set; }
    public MarketplaceListing? Listing { get; set; }

    public Guid BuyerId { get; set; }
    public Guid SellerId { get; set; }

    public OfferStatus Status { get; set; } = OfferStatus.Open;
    public decimal CurrentPrice { get; set; }
    public Guid LastProposerId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<ListingOfferRound> Rounds { get; set; } = new List<ListingOfferRound>();
}
