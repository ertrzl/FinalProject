using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// One "Öne Çıkar" use. Kept apart from the listing so deleting the listing does not give the seller a fresh boost:
// the weekly limit counts these rows per seller (CreatedAt = when the boost was used).
public class ListingBoost : BaseEntity
{
    public Guid SellerId { get; set; }
}
