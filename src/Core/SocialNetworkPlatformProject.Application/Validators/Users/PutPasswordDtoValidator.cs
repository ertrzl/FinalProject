using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Users;

namespace SocialNetworkPlatformProject.Application.Validators.Users;

public class PutPasswordDtoValidator : AbstractValidator<PutPasswordDto>
{
    public PutPasswordDtoValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage(_ => Loc.T("val.password.currentRequired"));

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage(_ => Loc.T("val.password.newRequired"))
            .MinimumLength(8).WithMessage(_ => Loc.T("val.password.short"))
            .MaximumLength(1024).WithMessage(_ => Loc.T("val.password.long"));

        RuleFor(x => x.ConfirmNewPassword)
            .Equal(x => x.NewPassword).WithMessage(_ => Loc.T("val.password.mismatch"));
    }
}
