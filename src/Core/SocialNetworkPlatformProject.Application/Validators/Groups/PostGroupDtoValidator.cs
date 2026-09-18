using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Groups;

namespace SocialNetworkPlatformProject.Application.Validators.Groups;

public class PostGroupDtoValidator : AbstractValidator<PostGroupDto>
{
    public PostGroupDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Group name is required.")
            .MaximumLength(100);

        RuleFor(x => x.Description).MaximumLength(1000);

        RuleFor(x => x.Privacy)
            .Must(p => p is "Public" or "Private")
            .WithMessage("Privacy must be either 'Public' or 'Private'.");
    }
}
