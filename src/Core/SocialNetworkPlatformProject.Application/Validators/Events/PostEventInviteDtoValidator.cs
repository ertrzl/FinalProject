using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Validators.Events;

public class PostEventInviteDtoValidator : AbstractValidator<PostEventInviteDto>
{
    public PostEventInviteDtoValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage(_ => Loc.T("val.eventInvite.user"));
    }
}
