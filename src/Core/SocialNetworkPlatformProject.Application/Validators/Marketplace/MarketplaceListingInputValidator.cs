using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Marketplace;

namespace SocialNetworkPlatformProject.Application.Validators.Marketplace;

public abstract class MarketplaceListingInputValidator<T> : AbstractValidator<T> where T : MarketplaceListingInputDto
{
    protected MarketplaceListingInputValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage(_ => Loc.T("val.listing.title"))
            .MaximumLength(150);

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage(_ => Loc.T("val.price.positive"))
            .LessThanOrEqualTo(100_000_000m).WithMessage(_ => Loc.T("val.price.high"))
            .PrecisionScale(18, 2, true).WithMessage(_ => Loc.T("val.price.decimals"));

        RuleFor(x => x.Currency)
            .Must(c => MarketplaceCurrencies.IsValid(c))
            .WithMessage(_ => Loc.T("val.listing.currency", string.Join(", ", MarketplaceCurrencies.All)));

        RuleFor(x => x.Category)
            .NotEmpty()
            .Must(c => MarketplaceCategories.All.Contains(c))
            .WithMessage(_ => Loc.T("val.listing.category", string.Join(", ", MarketplaceCategories.All)));

        RuleFor(x => x.Condition)
            .Must(c => MarketplaceConditions.All.Contains(c))
            .WithMessage(_ => Loc.T("val.listing.condition"));

        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);

        RuleFor(x => x.Images.Count)
            .LessThanOrEqualTo(MarketplaceLimits.MaxImagesPerListing)
            .WithMessage(_ => Loc.T("val.listing.photos", MarketplaceLimits.MaxImagesPerListing));
    }
}
