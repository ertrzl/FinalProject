using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Users;

namespace SocialNetworkPlatformProject.Application.Validators.Users;

public class ResendVerificationDtoValidator : AbstractValidator<ResendVerificationDto>
{
    public ResendVerificationDtoValidator()
    {
        RuleFor(x => x.EmailOrUserName)
            .NotEmpty().WithMessage(_ => Loc.T("val.email.required"))
            .MaximumLength(256);
    }
}
