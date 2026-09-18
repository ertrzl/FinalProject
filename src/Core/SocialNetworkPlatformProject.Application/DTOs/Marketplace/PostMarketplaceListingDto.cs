using Microsoft.AspNetCore.Http;

namespace SocialNetworkPlatformProject.Application.DTOs.Marketplace;

// "Ürün Sat" modal on marketplace.html
public class PostMarketplaceListingDto
{
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Description { get; set; }
    public IFormFile? Image { get; set; }
}
