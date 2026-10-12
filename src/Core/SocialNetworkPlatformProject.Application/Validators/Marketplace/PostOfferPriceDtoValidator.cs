using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Marketplace;

namespace SocialNetworkPlatformProject.Application.Validators.Marketplace;

// Shape only; the negotiation rules (price window, whose turn it is) depend on the thread and live in OfferService.
public class PostOfferPriceDtoValidator : AbstractValidator<PostOfferPriceDto>
{
    public PostOfferPriceDtoValidator()
    {
        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage(_ => Loc.T("val.price.positive"))
            .LessThanOrEqualTo(100_000_000m).WithMessage(_ => Loc.T("val.price.high"))
            .PrecisionScale(18, 2, true).WithMessage(_ => Loc.T("val.price.decimals"));
    }
}
