using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Validators.Events;

public class PutEventDtoValidator : EventInputValidator<PutEventDto>
{
    public PutEventDtoValidator()
    {
        RuleFor(x => x.StartsAt)
            .Must(startsAt => startsAt > DateTime.UtcNow).WithMessage("Event start date must be in the future.");
    }
}
