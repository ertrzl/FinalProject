using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Marketplace;

namespace SocialNetworkPlatformProject.Application.Validators.Marketplace;

public class PutListingStatusDtoValidator : AbstractValidator<PutListingStatusDto>
{
    public PutListingStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .Must(s => s is "Active" or "Sold")
            .WithMessage("Status must be either 'Active' or 'Sold'.");
    }
}
