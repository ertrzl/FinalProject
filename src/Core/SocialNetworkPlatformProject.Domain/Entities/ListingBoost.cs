using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class ListingBoost : BaseEntity
{
    public Guid SellerId { get; set; }
}
