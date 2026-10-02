using SocialNetworkPlatformProject.Application.DTOs.Marketplace;

namespace SocialNetworkPlatformProject.Application.Validators.Marketplace;

// The photo limit depends on how many photos the listing already has, so the service enforces it.
public class PutMarketplaceListingDtoValidator : MarketplaceListingInputValidator<PutMarketplaceListingDto>
{
}
