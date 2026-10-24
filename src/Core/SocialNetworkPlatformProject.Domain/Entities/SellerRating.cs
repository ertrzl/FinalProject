using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class SellerRating : BaseEntity
{
    public Guid SellerId { get; set; }
    public Guid BuyerId { get; set; }
    public Guid OfferId { get; set; }
    public string ListingTitle { get; set; } = string.Empty;

    public int Stars { get; set; }
    public string? Comment { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
