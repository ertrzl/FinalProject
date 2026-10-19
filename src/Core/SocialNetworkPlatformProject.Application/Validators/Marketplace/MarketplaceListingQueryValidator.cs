using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Marketplace;

namespace SocialNetworkPlatformProject.Application.Validators.Marketplace;

public class MarketplaceListingQueryValidator : AbstractValidator<MarketplaceListingQuery>
{
    public MarketplaceListingQueryValidator()
    {
        RuleFor(x => x.Sort)
            .Must(s => MarketplaceSort.All.Contains(s))
            .WithMessage(_ => Loc.T("val.listing.sort", string.Join(", ", MarketplaceSort.All)));

        RuleFor(x => x.Condition)
            .Must(c => MarketplaceConditions.All.Contains(c!))
            .When(x => !string.IsNullOrWhiteSpace(x.Condition))
            .WithMessage(_ => Loc.T("val.listing.condition"));

        RuleFor(x => x.MinPrice).GreaterThanOrEqualTo(0).When(x => x.MinPrice.HasValue);
        RuleFor(x => x.MaxPrice).GreaterThanOrEqualTo(0).When(x => x.MaxPrice.HasValue);

        RuleFor(x => x)
            .Must(x => x.MinPrice <= x.MaxPrice)
            .When(x => x.MinPrice.HasValue && x.MaxPrice.HasValue)
            .WithMessage(_ => Loc.T("val.listing.priceRange"));
    }
}
