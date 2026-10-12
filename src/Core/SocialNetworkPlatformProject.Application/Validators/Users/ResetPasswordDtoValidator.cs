using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Users;

namespace SocialNetworkPlatformProject.Application.Validators.Users;

public class ResetPasswordDtoValidator : AbstractValidator<ResetPasswordDto>
{
    public ResetPasswordDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(_ => Loc.T("val.email.required"))
            .EmailAddress().WithMessage(_ => Loc.T("val.email.invalid"))
            .MaximumLength(256);

        RuleFor(x => x.Token)
            .NotEmpty().WithMessage(_ => Loc.T("val.reset.code"))
            .MaximumLength(2048);

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage(_ => Loc.T("val.password.newRequired"))
            .MinimumLength(8).WithMessage(_ => Loc.T("val.password.short"));

        RuleFor(x => x.ConfirmNewPassword)
            .Equal(x => x.NewPassword).WithMessage(_ => Loc.T("val.password.mismatch"));
    }
}
