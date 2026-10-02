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

    public ICollection<ListingImage> Images { get; set; } = new List<ListingImage>();
}
