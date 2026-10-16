using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Validators.Events;

public class PostEventDtoValidator : EventInputValidator<PostEventDto>
{
    public PostEventDtoValidator()
    {
        RuleFor(x => x.StartsAt)
            .Must(startsAt => startsAt.ToUtc() > DateTime.UtcNow).WithMessage(_ => Loc.T("event.startFuture"));

        RuleFor(x => x.IsPrivate)
            .Equal(false).When(x => x.GroupId.HasValue)
            .WithMessage(_ => Loc.T("event.groupNotPrivate"));
    }
}
