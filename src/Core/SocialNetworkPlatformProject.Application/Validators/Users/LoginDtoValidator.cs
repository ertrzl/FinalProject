using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Users;

namespace SocialNetworkPlatformProject.Application.Validators.Users;

public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.EmailOrUserName).NotEmpty().WithMessage(_ => Loc.T("val.login.id"))
            .MaximumLength(256).WithMessage(_ => Loc.T("auth.badCredentials"));
        RuleFor(x => x.Password).NotEmpty().WithMessage(_ => Loc.T("val.password.required"))
            .MaximumLength(1024).WithMessage(_ => Loc.T("auth.badCredentials"));
    }
}
