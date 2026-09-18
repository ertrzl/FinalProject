using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Comments;

namespace SocialNetworkPlatformProject.Application.Validators.Comments;

public class PostCommentDtoValidator : AbstractValidator<PostCommentDto>
{
    public PostCommentDtoValidator()
    {
        RuleFor(x => x.PostId).NotEmpty();

        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Comment text cannot be empty.")
            .MaximumLength(1000).WithMessage("Comment text cannot exceed 1000 characters.");
    }
}
