using SocialNetworkPlatformProject.Application.DTOs.Games;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// Games between friends (games.html hub, game.html board). Every method answers with the game as the caller sees it,
// and every change is pushed live to both players ("GameUpdated").
public interface IGameService
{
    Task<GetGamesOverviewDto> GetOverviewAsync(Guid currentUserId);

    // Only the two players can open a game; anybody else is told it does not exist.
    Task<GetGameDto> GetAsync(Guid currentUserId, Guid gameId);

    // Invites a friend. Friends only, and only one invitation or running game per pair.
    Task<GetGameDto> InviteAsync(Guid currentUserId, PostGameDto dto);

    // The invited friend says yes: the marks are dealt by chance and X starts.
    Task<GetGameDto> AcceptAsync(Guid currentUserId, Guid gameId);

    Task<GetGameDto> DeclineAsync(Guid currentUserId, Guid gameId);

    // The inviter takes the invitation back before it is answered.
    Task<GetGameDto> CancelAsync(Guid currentUserId, Guid gameId);

    Task<GetGameDto> MoveAsync(Guid currentUserId, Guid gameId, PostGameMoveDto dto);

    // Rock-paper-scissors: puts out a hand for the round being played. The pick stays secret until the other side has
    // picked too; then the round is scored and both see it.
    Task<GetGameDto> ChooseAsync(Guid currentUserId, Guid gameId, PostGameChoiceDto dto);

    // Battleship: a random valid layout for the "Rastgele diz" button (not saved).
    GetRandomFleetDto GetRandomFleet();

    // Battleship: puts my whole fleet on my board (once). When both players have placed theirs, the battle starts.
    Task<GetGameDto> PlaceAsync(Guid currentUserId, Guid gameId, PostGamePlaceDto dto);

    // Battleship: a shot at the opponent's board. A hit lets me shoot again, a miss passes the turn.
    Task<GetGameDto> ShootAsync(Guid currentUserId, Guid gameId, PostGameShootDto dto);

    // Gives up: the other player wins.
    Task<GetGameDto> ResignAsync(Guid currentUserId, Guid gameId);
}
