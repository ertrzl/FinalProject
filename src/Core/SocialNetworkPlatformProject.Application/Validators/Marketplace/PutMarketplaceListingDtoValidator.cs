using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Marketplace;

namespace SocialNetworkPlatformProject.Application.Validators.Marketplace;

public class PutMarketplaceListingDtoValidator : MarketplaceListingInputValidator<PutMarketplaceListingDto>
{
    public PutMarketplaceListingDtoValidator()
    {
        RuleFor(x => x)
            .Must(x => !(x.RemoveImage && x.Image != null))
            .WithMessage("Cannot both remove and upload an image.");
    }
}
