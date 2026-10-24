using Microsoft.AspNetCore.Http;

namespace SocialNetworkPlatformProject.Application.DTOs.Marketplace;

public abstract class MarketplaceListingInputDto
{
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Description { get; set; }
    public string Condition { get; set; } = string.Empty;
    public List<IFormFile> Images { get; set; } = new();
}
