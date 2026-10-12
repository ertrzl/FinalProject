using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Users;

namespace SocialNetworkPlatformProject.Application.Validators.Users;

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    private const int MaxAgeYears = 120;

    public RegisterDtoValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage(_ => Loc.T("val.fullName.required"))
            .MaximumLength(100);

        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage(_ => Loc.T("val.userName.required"))
            .MaximumLength(50)
            .Matches("^[a-zA-Z0-9._]+$").WithMessage(_ => Loc.T("val.userName.chars"));

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(_ => Loc.T("val.email.required"))
            .EmailAddress().WithMessage(_ => Loc.T("val.email.invalid"));

        RuleFor(x => x.BirthDate)
            .Must(date => date!.Value.Date <= DateTime.UtcNow.Date)
                .WithMessage(_ => Loc.T("val.birth.future"))
            .Must(date => date!.Value.Date >= DateTime.UtcNow.Date.AddYears(-MaxAgeYears))
                .WithMessage(_ => Loc.T("val.birth.invalid"))
            .When(x => x.BirthDate.HasValue);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(_ => Loc.T("val.password.required"))
            .MinimumLength(8).WithMessage(_ => Loc.T("val.password.short"));
    }
}
