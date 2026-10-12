using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Users;

namespace SocialNetworkPlatformProject.Application.Validators.Users;

public class DeleteAccountDtoValidator : AbstractValidator<DeleteAccountDto>
{
    public DeleteAccountDtoValidator()
    {
        RuleFor(x => x.Password).NotEmpty().WithMessage(_ => Loc.T("val.password.required"));
    }
}
