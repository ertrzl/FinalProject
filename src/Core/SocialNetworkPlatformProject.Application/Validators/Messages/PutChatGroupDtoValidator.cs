using FluentValidation;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Messages;

namespace SocialNetworkPlatformProject.Application.Validators.Messages;

public class PutChatGroupDtoValidator : AbstractValidator<PutChatGroupDto>
{
    public PutChatGroupDtoValidator()
    {
        // Left out means unchanged; a name that is sent must be a real one.
        RuleFor(x => x.Title)
            .Must(title => title == null || !string.IsNullOrWhiteSpace(title)).WithMessage(_ => Loc.T("val.chatGroup.title"))
            .MaximumLength(ChatGroupLimits.TitleLength).WithMessage(_ => Loc.T("val.chatGroup.title"));
    }
}
