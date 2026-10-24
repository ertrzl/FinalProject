using SocialNetworkPlatformProject.Application.DTOs.Games;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IGameService
{
    Task<GetGamesOverviewDto> GetOverviewAsync(Guid currentUserId);

    Task<GetGameDto> GetAsync(Guid currentUserId, Guid gameId);

    Task<GetGameDto> InviteAsync(Guid currentUserId, PostGameDto dto);

    Task<GetGameDto> AcceptAsync(Guid currentUserId, Guid gameId);

    Task<GetGameDto> DeclineAsync(Guid currentUserId, Guid gameId);

    Task<GetGameDto> CancelAsync(Guid currentUserId, Guid gameId);

    Task<GetGameDto> MoveAsync(Guid currentUserId, Guid gameId, PostGameMoveDto dto);

    Task<GetGameDto> ChooseAsync(Guid currentUserId, Guid gameId, PostGameChoiceDto dto);

    GetRandomFleetDto GetRandomFleet();

    Task<GetGameDto> PlaceAsync(Guid currentUserId, Guid gameId, PostGamePlaceDto dto);

    Task<GetGameDto> ShootAsync(Guid currentUserId, Guid gameId, PostGameShootDto dto);

    Task<GetGameDto> ResignAsync(Guid currentUserId, Guid gameId);
}
