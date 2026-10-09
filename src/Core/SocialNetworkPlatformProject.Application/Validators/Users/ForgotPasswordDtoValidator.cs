using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Users;

namespace SocialNetworkPlatformProject.Application.Validators.Users;

public class ForgotPasswordDtoValidator : AbstractValidator<ForgotPasswordDto>
{
    public ForgotPasswordDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(256);
    }
}
