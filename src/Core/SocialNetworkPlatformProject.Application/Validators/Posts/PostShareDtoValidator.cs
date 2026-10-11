using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Posts;

namespace SocialNetworkPlatformProject.Application.Validators.Posts;

public class PostShareDtoValidator : AbstractValidator<PostShareDto>
{
    public PostShareDtoValidator()
    {
        RuleFor(x => x.Text)
            .MaximumLength(5000).WithMessage("Text cannot exceed 5000 characters.");

        RuleFor(x => x.Privacy)
            .Must(p => p is "Public" or "FriendsOnly")
            .WithMessage("Privacy must be either 'Public' or 'FriendsOnly'.");
    }
}
