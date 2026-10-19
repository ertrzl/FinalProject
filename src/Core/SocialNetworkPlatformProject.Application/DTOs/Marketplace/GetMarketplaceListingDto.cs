namespace SocialNetworkPlatformProject.Application.DTOs.Marketplace;

// marketplace.html listing cards + detail modal
public class GetMarketplaceListingDto
{
    public Guid Id { get; set; }

    public Guid SellerId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public string? SellerAvatarUrl { get; set; }
    public double? SellerRatingAverage { get; set; } // null until the seller has been rated
    public int SellerRatingCount { get; set; }

    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; } // cover photo (first of Images), handy for grid cards
    public List<ListingImageDto> Images { get; set; } = new();
    public string Status { get; set; } = "Active"; // "Active" / "Reserved" / "Sold"
    public string Condition { get; set; } = "Good"; // "New" / "LikeNew" / "Good" / "Fair"

    // Lifetime: hidden from the public lists once ExpiresAt has passed (IsExpired), until the seller renews it.
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired { get; set; }
    public bool IsFeatured { get; set; } // "Öne Çıkan": the seller boosted it and the day is not over
    public DateTime? FeaturedUntil { get; set; }

    // What the viewer may do. The server applies every rule; the page only shows or hides the buttons.
    public bool CanMessageSeller { get; set; } // only filled in by GET /api/marketplace/{id} (the messaging rule is checked per person)
    public bool CanRenew { get; set; }   // seller only: expired or about to expire, and not renewed in the last 24 hours
    public bool CanFeature { get; set; } // seller only: no other boost running, none used in the last 7 days, the listing is on sale
    public DateTime? NextRenewAt { get; set; }   // seller only: when "Yenile" opens up again (the 24-hour limit), if it is closed for that reason
    public DateTime? NextFeatureAt { get; set; } // seller only: when the weekly limit ends, if it is the reason "Öne Çıkar" is closed
    public bool IsSavedByCurrentUser { get; set; }
    public Guid? MyOpenOfferId { get; set; } // the viewer's own running negotiation on this listing, if any
    public int OpenOfferCount { get; set; }  // seller's view: negotiations still waiting on someone
    public DateTime CreatedAt { get; set; }
}
