using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Users;

namespace SocialNetworkPlatformProject.Application.Validators.Users;

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    private const int MaxAgeYears = 120;

    public RegisterDtoValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100);

        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Username is required.")
            .MaximumLength(50)
            .Matches("^[a-zA-Z0-9._]+$").WithMessage("Username can only contain letters, numbers, dots, and underscores.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.BirthDate)
            .Must(date => date!.Value.Date <= DateTime.UtcNow.Date)
                .WithMessage("Birth date cannot be in the future.")
            .Must(date => date!.Value.Date >= DateTime.UtcNow.Date.AddYears(-MaxAgeYears))
                .WithMessage("Birth date is not valid.")
            .When(x => x.BirthDate.HasValue);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.");
    }
}
