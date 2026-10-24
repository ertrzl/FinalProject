using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Messages;

namespace SocialNetworkPlatformProject.Application.Validators.Messages;

public class PostMessageDtoValidator : AbstractValidator<PostMessageDto>
{
    public PostMessageDtoValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage(_ => Loc.T("val.message.empty"))
            .MaximumLength(2000);

        RuleFor(x => x.Type)
            .Must(type => string.IsNullOrEmpty(type) || type == "Text" || type == "Sticker")
            .WithMessage(_ => Loc.T("val.message.type"));

        RuleFor(x => x.Text)
            .MaximumLength(16).When(x => x.Type == "Sticker")
            .WithMessage(_ => Loc.T("val.message.sticker"));

        RuleFor(x => x)
            .Must(x => x.ConversationId.HasValue || x.ReceiverId.HasValue)
            .WithMessage(_ => Loc.T("message.target"));
    }
}
