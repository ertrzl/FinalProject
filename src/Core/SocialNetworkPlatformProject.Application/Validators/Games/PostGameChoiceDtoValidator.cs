using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Games;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Application.Validators.Games;

public class PostGameChoiceDtoValidator : AbstractValidator<PostGameChoiceDto>
{
    public PostGameChoiceDtoValidator()
    {
        RuleFor(x => x.Choice)
            .Must(c => Enum.TryParse<RpsChoice>(c, true, out var parsed) && Enum.IsDefined(parsed))
            .WithMessage(_ => Loc.T("val.game.choice"));
    }
}
