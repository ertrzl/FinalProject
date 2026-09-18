using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Users;

namespace SocialNetworkPlatformProject.Application.Validators.Users;

public class PutUserProfileDtoValidator : AbstractValidator<PutUserProfileDto>
{
    public PutUserProfileDtoValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Bio).MaximumLength(500);
        RuleFor(x => x.Location).MaximumLength(150);
        RuleFor(x => x.Occupation).MaximumLength(150);
        RuleFor(x => x.Education).MaximumLength(150);
    }
}
