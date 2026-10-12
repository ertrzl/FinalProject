using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Posts;

namespace SocialNetworkPlatformProject.Application.Validators.Posts;

public class PostShareDtoValidator : AbstractValidator<PostShareDto>
{
    public PostShareDtoValidator()
    {
        RuleFor(x => x.Text)
            .MaximumLength(5000).WithMessage(_ => Loc.T("val.share.long"));

        RuleFor(x => x.Privacy)
            .Must(p => p is "Public" or "FriendsOnly")
            .WithMessage(_ => Loc.T("post.privacy"));
    }
}
