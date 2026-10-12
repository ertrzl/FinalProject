using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Marketplace;

namespace SocialNetworkPlatformProject.Application.Validators.Marketplace;

public class PostSellerRatingDtoValidator : AbstractValidator<PostSellerRatingDto>
{
    public PostSellerRatingDtoValidator()
    {
        RuleFor(x => x.Stars)
            .InclusiveBetween(1, 5).WithMessage(_ => Loc.T("val.rating.stars"));

        RuleFor(x => x.Comment)
            .MaximumLength(500).WithMessage(_ => Loc.T("val.rating.comment"));
    }
}
