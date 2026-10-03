using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Validators.Events;

public class PostEventDtoValidator : EventInputValidator<PostEventDto>
{
    public PostEventDtoValidator()
    {
        RuleFor(x => x.StartsAt)
            .Must(startsAt => startsAt > DateTime.UtcNow).WithMessage("Event start date must be in the future.");
    }
}
