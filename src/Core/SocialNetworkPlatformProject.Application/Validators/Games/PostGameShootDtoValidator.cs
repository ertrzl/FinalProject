using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Games;

namespace SocialNetworkPlatformProject.Application.Validators.Games;

public class PostGameShootDtoValidator : AbstractValidator<PostGameShootDto>
{
    public PostGameShootDtoValidator()
    {
        RuleFor(x => x.Cell)
            .InclusiveBetween(0, 99).WithMessage(_ => Loc.T("val.game.shootCell"));
    }
}
