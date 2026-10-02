namespace SocialNetworkPlatformProject.Application.DTOs.Marketplace;

// "İlanı Düzenle" — same modal as selling. A new Image replaces the old one; RemoveImage clears it.
public class PutMarketplaceListingDto : MarketplaceListingInputDto
{
    public bool RemoveImage { get; set; }
}
