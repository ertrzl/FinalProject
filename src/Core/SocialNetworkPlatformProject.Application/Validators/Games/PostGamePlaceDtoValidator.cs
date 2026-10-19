using FluentValidation;
using SocialNetworkPlatformProject.Application.DTOs.Games;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Application.Validators.Games;

public class PostGamePlaceDtoValidator : AbstractValidator<PostGamePlaceDto>
{
    public PostGamePlaceDtoValidator()
    {
        // One of each of the five ships; where they may lie (inside the board, not on each other) is the game's rule, checked in the service.
        RuleFor(x => x.Ships)
            .NotNull().WithMessage(_ => Loc.T("val.game.ships"))
            .Must(ships => ships != null && ships.Count == Enum.GetValues<ShipKind>().Length).WithMessage(_ => Loc.T("val.game.ships"));

        RuleForEach(x => x.Ships).ChildRules(ship =>
        {
            ship.RuleFor(s => s.Ship)
                .Must(name => Enum.TryParse<ShipKind>(name, true, out var kind) && Enum.IsDefined(kind)).WithMessage(_ => Loc.T("val.game.ships"));
            ship.RuleFor(s => s.Row).InclusiveBetween(0, 9).WithMessage(_ => Loc.T("val.game.ships"));
            ship.RuleFor(s => s.Col).InclusiveBetween(0, 9).WithMessage(_ => Loc.T("val.game.ships"));
        });
    }
}
