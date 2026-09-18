using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Messages;

namespace SocialNetworkPlatformProject.Application.Validators.Messages;

public class PostMessageDtoValidator : AbstractValidator<PostMessageDto>
{
    public PostMessageDtoValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Message text cannot be empty.")
            .MaximumLength(2000);

        RuleFor(x => x)
            .Must(x => x.ConversationId.HasValue || x.ReceiverId.HasValue)
            .WithMessage("Either ConversationId or ReceiverId must be provided.");
    }
}
