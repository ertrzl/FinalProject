using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// A buyer's star rating (1-5) of a seller after an accepted marketplace deal; one rating per deal.
//
// OfferId is deliberately a plain id without a foreign key: deleting the listing deletes its offers, and a
// seller must not be able to wipe unfavourable reviews by deleting the listing. ListingTitle is a snapshot
// for the same reason (the review stays readable after the listing is gone).
public class SellerRating : BaseEntity
{
    public Guid SellerId { get; set; }
    public Guid BuyerId { get; set; }
    public Guid OfferId { get; set; }
    public string ListingTitle { get; set; } = string.Empty;

    public int Stars { get; set; }
    public string? Comment { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
