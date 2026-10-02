using FluentValidation;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Marketplace;

namespace SocialNetworkPlatformProject.Application.Validators.Marketplace;

// Rules shared by creating and editing a listing.
public abstract class MarketplaceListingInputValidator<T> : AbstractValidator<T> where T : MarketplaceListingInputDto
{
    protected MarketplaceListingInputValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(150);

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero.")
            .LessThanOrEqualTo(100_000_000m).WithMessage("Price is too high.")
            .PrecisionScale(18, 2, true).WithMessage("Price can have at most two decimal places.");

        RuleFor(x => x.Category)
            .NotEmpty()
            .Must(c => MarketplaceCategories.All.Contains(c))
            .WithMessage("Category must be one of: " + string.Join(", ", MarketplaceCategories.All));

        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
