using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Validators.Events;

public class PostEventDtoValidator : AbstractValidator<PostEventDto>
{
    public PostEventDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Event title is required.")
            .MaximumLength(150);

        RuleFor(x => x.StartsAt)
            .GreaterThan(DateTime.UtcNow).WithMessage("Event start date must be in the future.");

        RuleFor(x => x.Description).MaximumLength(2000);
    }
}
