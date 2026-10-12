using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Validators.Events;

public class PostEventCommentDtoValidator : AbstractValidator<PostEventCommentDto>
{
    public const int MaxContentLength = 1000;

    public PostEventCommentDtoValidator()
    {
        RuleFor(x => x.Content)
            .Must(content => !string.IsNullOrWhiteSpace(content)).WithMessage(_ => Loc.T("val.message.empty"))
            .MaximumLength(MaxContentLength).WithMessage(_ => Loc.T("val.eventMessage.long", MaxContentLength));
    }
}
