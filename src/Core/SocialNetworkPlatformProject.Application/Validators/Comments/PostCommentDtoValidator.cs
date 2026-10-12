using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Comments;

namespace SocialNetworkPlatformProject.Application.Validators.Comments;

public class PostCommentDtoValidator : AbstractValidator<PostCommentDto>
{
    public PostCommentDtoValidator()
    {
        RuleFor(x => x.PostId).NotEmpty();

        RuleFor(x => x.Text)
            .NotEmpty().WithMessage(_ => Loc.T("val.comment.empty"))
            .MaximumLength(1000).WithMessage(_ => Loc.T("val.comment.long"));
    }
}
