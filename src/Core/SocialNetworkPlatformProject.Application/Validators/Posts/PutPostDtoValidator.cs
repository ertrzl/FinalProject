using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Posts;

namespace SocialNetworkPlatformProject.Application.Validators.Posts;

public class PutPostDtoValidator : AbstractValidator<PutPostDto>
{
    public PutPostDtoValidator()
    {
        RuleFor(x => x.Text)
            .MaximumLength(5000).WithMessage("Post text cannot exceed 5000 characters.");

        RuleFor(x => x.Privacy)
            .Must(p => p is "Public" or "FriendsOnly")
            .WithMessage("Privacy must be either 'Public' or 'FriendsOnly'.");
    }
}
