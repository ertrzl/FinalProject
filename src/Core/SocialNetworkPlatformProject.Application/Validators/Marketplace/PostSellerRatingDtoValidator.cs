using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Marketplace;

namespace SocialNetworkPlatformProject.Application.Validators.Marketplace;

public class PostSellerRatingDtoValidator : AbstractValidator<PostSellerRatingDto>
{
    public PostSellerRatingDtoValidator()
    {
        RuleFor(x => x.Stars)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5 stars.");

        RuleFor(x => x.Comment)
            .MaximumLength(500).WithMessage("Comment cannot exceed 500 characters.");
    }
}
