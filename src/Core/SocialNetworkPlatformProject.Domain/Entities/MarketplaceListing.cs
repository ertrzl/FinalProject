using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

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

    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddDays(30);
    public DateTime? RenewedAt { get; set; }
    public DateTime? FeaturedUntil { get; set; }

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<ListingImage> Images { get; set; } = new List<ListingImage>();
}
