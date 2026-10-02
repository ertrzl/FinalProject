namespace SocialNetworkPlatformProject.Application.DTOs.Marketplace;

// "İlanı Düzenle" — same modal as selling. Images are added to the existing ones; RemoveImageIds drops specific ones.
public class PutMarketplaceListingDto : MarketplaceListingInputDto
{
    public List<Guid> RemoveImageIds { get; set; } = new();
}
