using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Marketplace;

namespace SocialNetworkPlatformProject.Application.Validators.Marketplace;

public class PostMarketplaceListingDtoValidator : AbstractValidator<PostMarketplaceListingDto>
{
    private static readonly string[] AllowedCategories =
        { "Elektronik", "Ev Eşyası", "Giyim", "Kitap", "Spor", "Araç" };

    public PostMarketplaceListingDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(150);

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero.");

        RuleFor(x => x.Category)
            .NotEmpty()
            .Must(c => AllowedCategories.Contains(c))
            .WithMessage("Category must be one of: " + string.Join(", ", AllowedCategories));

        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
