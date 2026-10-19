using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

// marketplace.html listing cards + "Ürün Sat" modal (js/marketplace.js)
public class MarketplaceListing : BaseEntity
{
    public Guid SellerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Description { get; set; }
    public ListingStatus Status { get; set; } = ListingStatus.Active;
    public ListingCondition Condition { get; set; } = ListingCondition.Good;

    // A listing lives 30 days; after that it is hidden from the public lists until its seller renews it.
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(30);
    public DateTime? RenewedAt { get; set; } // last "Yenile", for the once-a-day limit
    public DateTime? FeaturedUntil { get; set; } // "Öne Çıkar": shown first in the grid until this moment

    // Concurrency token: two people changing the same listing at once (e.g. two accepted offers) can't both win.
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<ListingImage> Images { get; set; } = new List<ListingImage>();
}
