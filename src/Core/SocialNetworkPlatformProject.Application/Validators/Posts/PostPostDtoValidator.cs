using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Posts;

namespace SocialNetworkPlatformProject.Application.Validators.Posts;

public class PostPostDtoValidator : AbstractValidator<PostPostDto>
{
    public PostPostDtoValidator()
    {
        // Matches publishPost()'s own check in app.js: a post needs text OR an image, not necessarily both.
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Text) || x.Image != null)
            .WithMessage("A post must have either text or an image.");

        RuleFor(x => x.Text)
            .MaximumLength(5000).WithMessage("Post text cannot exceed 5000 characters.");

        RuleFor(x => x.Privacy)
            .Must(p => p is "Public" or "FriendsOnly")
            .WithMessage("Privacy must be either 'Public' or 'FriendsOnly'.");
    }
}
