namespace SocialNetworkPlatformProject.Application.DTOs.Marketplace;

// marketplace.html listing cards + detail modal
public class GetMarketplaceListingDto
{
    public Guid Id { get; set; }

    public Guid SellerId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public string? SellerAvatarUrl { get; set; }

    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; } // cover photo (first of Images), handy for grid cards
    public List<ListingImageDto> Images { get; set; } = new();
    public string Status { get; set; } = "Active"; // "Active" / "Sold"
    public bool IsSavedByCurrentUser { get; set; }
    public Guid? MyOpenOfferId { get; set; } // the viewer's own running negotiation on this listing, if any
    public int OpenOfferCount { get; set; }  // seller's view: negotiations still waiting on someone
    public DateTime CreatedAt { get; set; }
}
