using FluentValidation;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Messages;

namespace SocialNetworkPlatformProject.Application.Validators.Messages;

public class PostChatGroupDtoValidator : AbstractValidator<PostChatGroupDto>
{
    public PostChatGroupDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage(_ => Loc.T("val.chatGroup.title"))
            .MaximumLength(ChatGroupLimits.TitleLength).WithMessage(_ => Loc.T("val.chatGroup.title"));

        RuleFor(x => x.MemberIds)
            .NotNull().WithMessage(_ => Loc.T("val.chatGroup.members", ChatGroupLimits.MinOtherMembers))
            .Must(ids => ids != null && ids.Distinct().Count() >= ChatGroupLimits.MinOtherMembers)
            .WithMessage(_ => Loc.T("val.chatGroup.members", ChatGroupLimits.MinOtherMembers))
            .Must(ids => ids == null || ids.Distinct().Count() < ChatGroupLimits.MaxMembers)
            .WithMessage(_ => Loc.T("chatGroup.tooMany", ChatGroupLimits.MaxMembers));
    }
}
