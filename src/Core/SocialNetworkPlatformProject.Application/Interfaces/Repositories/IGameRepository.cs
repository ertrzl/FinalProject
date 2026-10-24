using SocialNetworkPlatformProject.Application.DTOs.Games;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IGameRepository : IRepository<Game>
{
    Task<int> ExpireStaleForPairAsync(string pairKey, DateTime invitedBefore, DateTime idleSince, DateTime now);

    Task<List<Game>> GetIncomingInvitesAsync(Guid userId, DateTime createdAfter);

    Task<List<Game>> GetSentInvitesAsync(Guid userId, DateTime createdAfter);

    Task<List<Game>> GetActiveAsync(Guid userId, DateTime updatedAfter);

    Task<List<Game>> GetRecentFinishedAsync(Guid userId, int take);

    Task ReloadAsync(Game game);

    Task<GetGameStatsDto> GetStatsAsync(Guid userId);
}
