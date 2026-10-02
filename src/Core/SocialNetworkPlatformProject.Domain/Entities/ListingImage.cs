using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// One photo of a marketplace listing; the lowest SortOrder is the cover shown on the grid card.
public class ListingImage : BaseEntity
{
    public Guid ListingId { get; set; }
    public MarketplaceListing? Listing { get; set; }

    public string Url { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
