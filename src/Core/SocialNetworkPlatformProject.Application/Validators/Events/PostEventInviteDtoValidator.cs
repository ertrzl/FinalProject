using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Validators.Events;

public class PostEventInviteDtoValidator : AbstractValidator<PostEventInviteDto>
{
    public PostEventInviteDtoValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("Davet edilecek kişi seçilmeli.");
    }
}
