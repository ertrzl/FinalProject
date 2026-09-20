using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Validators.Events;

public class PutEventStatusDtoValidator : AbstractValidator<PutEventStatusDto>
{
    public PutEventStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .Must(s => s is "Going" or "Interested" or "None")
            .WithMessage("Status must be 'Going', 'Interested' or 'None'.");
    }
}
