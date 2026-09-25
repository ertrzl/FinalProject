using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Messages;

namespace SocialNetworkPlatformProject.Application.Validators.Messages;

public class PostMessageImageDtoValidator : AbstractValidator<PostMessageImageDto>
{
    public PostMessageImageDtoValidator()
    {
        RuleFor(x => x.Image)
            .NotNull().WithMessage("Choose a photo to send.");

        RuleFor(x => x.Text)
            .MaximumLength(2000);

        RuleFor(x => x)
            .Must(x => x.ConversationId.HasValue || x.ReceiverId.HasValue)
            .WithMessage("Either ConversationId or ReceiverId must be provided.");
    }
}
