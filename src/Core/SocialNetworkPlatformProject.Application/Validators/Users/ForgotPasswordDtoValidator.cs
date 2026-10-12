using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Users;

namespace SocialNetworkPlatformProject.Application.Validators.Users;

public class ForgotPasswordDtoValidator : AbstractValidator<ForgotPasswordDto>
{
    public ForgotPasswordDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(_ => Loc.T("val.email.required"))
            .EmailAddress().WithMessage(_ => Loc.T("val.email.invalid"))
            .MaximumLength(256);
    }
}
