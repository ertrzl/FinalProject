using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Users;

namespace SocialNetworkPlatformProject.Application.Validators.Users;

public class VerifyEmailDtoValidator : AbstractValidator<VerifyEmailDto>
{
    public VerifyEmailDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(_ => Loc.T("val.email.required"))
            .EmailAddress().WithMessage(_ => Loc.T("val.email.invalid"))
            .MaximumLength(256);

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage(_ => Loc.T("emailVerify.invalidLink"))
            .MaximumLength(2048);
    }
}
