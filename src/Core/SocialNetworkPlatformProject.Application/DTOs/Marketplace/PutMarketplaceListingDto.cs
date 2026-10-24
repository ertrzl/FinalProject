namespace SocialNetworkPlatformProject.Application.DTOs.Marketplace;

public class PutMarketplaceListingDto : MarketplaceListingInputDto
{
    public List<Guid> RemoveImageIds { get; set; } = new();
}
