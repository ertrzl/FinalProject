using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Validators.Events;

public class PostEventCommentDtoValidator : AbstractValidator<PostEventCommentDto>
{
    public const int MaxContentLength = 1000;

    public PostEventCommentDtoValidator()
    {
        RuleFor(x => x.Content)
            .Must(content => !string.IsNullOrWhiteSpace(content)).WithMessage("Mesaj boş olamaz.")
            .MaximumLength(MaxContentLength).WithMessage($"Mesaj en fazla {MaxContentLength} karakter olabilir.");
    }
}
