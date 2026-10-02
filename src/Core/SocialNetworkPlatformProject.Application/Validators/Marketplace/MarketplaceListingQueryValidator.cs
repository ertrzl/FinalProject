using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Marketplace;

namespace SocialNetworkPlatformProject.Application.Validators.Marketplace;

public class MarketplaceListingQueryValidator : AbstractValidator<MarketplaceListingQuery>
{
    public MarketplaceListingQueryValidator()
    {
        RuleFor(x => x.Sort)
            .Must(s => MarketplaceSort.All.Contains(s))
            .WithMessage("Sort must be one of: " + string.Join(", ", MarketplaceSort.All));

        RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0).When(x => x.MinPrice.HasValue);
        RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(0).When(x => x.MaxPrice.HasValue);

        RuleFor(x => x)
            .Must(x => x.MinPrice <= x.MaxPrice)
            .When(x => x.MinPrice.HasValue && x.MaxPrice.HasValue)
            .WithMessage("Minimum price can't be higher than maximum price.");
    }
}
