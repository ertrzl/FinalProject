using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Messages;

namespace SocialNetworkPlatformProject.Application.Validators.Messages;

public class PostChatGroupMembersDtoValidator : AbstractValidator<PostChatGroupMembersDto>
{
    public PostChatGroupMembersDtoValidator()
    {
        RuleFor(x => x.UserIds)
            .NotNull().WithMessage(_ => Loc.T("val.chatGroup.add"))
            .Must(ids => ids != null && ids.Count > 0).WithMessage(_ => Loc.T("val.chatGroup.add"));
    }
}
