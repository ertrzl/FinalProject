using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class SavedListing : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ListingId { get; set; }
    public MarketplaceListing? Listing { get; set; }
}
