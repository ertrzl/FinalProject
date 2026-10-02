using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Marketplace;

namespace SocialNetworkPlatformProject.Application.Validators.Marketplace;

// Shape only; the negotiation rules (price window, whose turn it is) depend on the thread and live in OfferService.
public class PostOfferPriceDtoValidator : AbstractValidator<PostOfferPriceDto>
{
    public PostOfferPriceDtoValidator()
    {
        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero.")
            .LessThanOrEqualTo(100_000_000m).WithMessage("Price is too high.")
            .PrecisionScale(18, 2, true).WithMessage("Price can have at most two decimal places.");
    }
}
