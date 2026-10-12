using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Posts;

namespace SocialNetworkPlatformProject.Application.Validators.Posts;

public class PutPostDtoValidator : AbstractValidator<PutPostDto>
{
    public PutPostDtoValidator()
    {
        RuleFor(x => x.Text)
            .MaximumLength(5000).WithMessage(_ => Loc.T("val.post.long"));

        RuleFor(x => x.Privacy)
            .Must(p => p is "Public" or "FriendsOnly")
            .WithMessage(_ => Loc.T("post.privacy"));
    }
}
