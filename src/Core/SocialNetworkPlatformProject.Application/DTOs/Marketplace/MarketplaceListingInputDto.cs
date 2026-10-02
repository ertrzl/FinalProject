using Microsoft.AspNetCore.Http;

namespace SocialNetworkPlatformProject.Application.DTOs.Marketplace;

// The fields a seller fills in, shared by "Ürün Sat" (create) and "İlanı Düzenle" (update).
public abstract class MarketplaceListingInputDto
{
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Description { get; set; }
    public IFormFile? Image { get; set; }
}
