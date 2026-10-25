namespace SocialNetworkPlatformProject.Application.DTOs.Marketplace;

public class GetMarketplaceListingDto
{
    public Guid Id { get; set; }

    public Guid SellerId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public string? SellerAvatarUrl { get; set; }
    public double? SellerRatingAverage { get; set; }
    public int SellerRatingCount { get; set; }

    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = MarketplaceCurrencies.Default;
    public string Category { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public List<ListingImageDto> Images { get; set; } = new();
    public string Status { get; set; } = "Active";
    public string Condition { get; set; } = "Good";

    public DateTime ExpiresAt { get; set; }
    public bool IsExpired { get; set; }
    public bool IsFeatured { get; set; }
    public DateTime? FeaturedUntil { get; set; }

    public bool CanMessageSeller { get; set; }
    public bool CanRenew { get; set; }
    public bool CanFeature { get; set; }
    public DateTime? NextRenewAt { get; set; }
    public DateTime? NextFeatureAt { get; set; }
    public bool IsSavedByCurrentUser { get; set; }
    public Guid? MyOpenOfferId { get; set; }
    public int OpenOfferCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
