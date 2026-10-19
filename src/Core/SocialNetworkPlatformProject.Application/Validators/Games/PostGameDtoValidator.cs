using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Games;

namespace SocialNetworkPlatformProject.Application.Validators.Games;

public class PostGameDtoValidator : AbstractValidator<PostGameDto>
{
    public PostGameDtoValidator()
    {
        RuleFor(x => x.OpponentId)
            .NotEmpty().WithMessage(_ => Loc.T("val.game.opponent"));
    }
}
