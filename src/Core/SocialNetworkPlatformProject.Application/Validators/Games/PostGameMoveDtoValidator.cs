using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Games;

namespace SocialNetworkPlatformProject.Application.Validators.Games;

public class PostGameMoveDtoValidator : AbstractValidator<PostGameMoveDto>
{
    public PostGameMoveDtoValidator()
    {
        RuleFor(x => x.Cell)
            .InclusiveBetween(0, 8).WithMessage(_ => Loc.T("val.game.cell"));
    }
}
