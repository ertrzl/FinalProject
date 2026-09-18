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
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
