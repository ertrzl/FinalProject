using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Groups;

namespace SocialNetworkPlatformProject.Application.Validators.Groups;

public class PutGroupDtoValidator : AbstractValidator<PutGroupDto>
{
    public PutGroupDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(_ => Loc.T("val.group.name"))
            .MaximumLength(100);

        RuleFor(x => x.Description).MaximumLength(1000);

        RuleFor(x => x.Privacy)
            .Must(p => p is "Public" or "Private")
            .WithMessage(_ => Loc.T("group.privacy"));

        RuleFor(x => x)
            .Must(x => !(x.RemoveCoverImage && x.CoverImage != null))
            .WithMessage(_ => Loc.T("val.group.cover"));

        RuleFor(x => x)
            .Must(x => !(x.RemoveAvatar && x.Avatar != null))
            .WithMessage(_ => Loc.T("val.group.avatar"));
    }
}
