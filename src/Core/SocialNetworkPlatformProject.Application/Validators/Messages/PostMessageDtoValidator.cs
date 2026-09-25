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

        RuleFor(x => x.Type)
            .Must(type => string.IsNullOrEmpty(type) || type == "Text" || type == "Sticker")
            .WithMessage("Type must be either 'Text' or 'Sticker'.");

        // A sticker is just one emoji (which can be several UTF-16 characters), never a long message.
        RuleFor(x => x.Text)
            .MaximumLength(16).When(x => x.Type == "Sticker")
            .WithMessage("A sticker can only be a single emoji.");

        RuleFor(x => x)
            .Must(x => x.ConversationId.HasValue || x.ReceiverId.HasValue)
            .WithMessage("Either ConversationId or ReceiverId must be provided.");
    }
}
