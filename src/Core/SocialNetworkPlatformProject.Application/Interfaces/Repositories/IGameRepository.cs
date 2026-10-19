using SocialNetworkPlatformProject.Application.DTOs.Games;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IGameRepository : IRepository<Game>
{
    // Closes the games of one pair that nobody answered or played any more: invitations created before
    // `invitedBefore` and running games last touched before `idleSince` become Expired. Returns how many changed.
    Task<int> ExpireStaleForPairAsync(string pairKey, DateTime invitedBefore, DateTime idleSince, DateTime now);

    // Invitations waiting for this person's answer, newest first (older than `createdAfter` have run out).
    Task<List<Game>> GetIncomingInvitesAsync(Guid userId, DateTime createdAfter);

    // Invitations this person sent that nobody answered yet, newest first.
    Task<List<Game>> GetSentInvitesAsync(Guid userId, DateTime createdAfter);

    // The running games this person is in, most recently played first (idle since before `updatedAfter` are left out).
    Task<List<Game>> GetActiveAsync(Guid userId, DateTime updatedAfter);

    // The newest finished games of this person.
    Task<List<Game>> GetRecentFinishedAsync(Guid userId, int take);

    // Throws away what was changed on a game in memory and reads it from the database again (after two players
    // changed it at the same moment, so the change can be made again on the newest state).
    Task ReloadAsync(Game game);

    // Wins, losses and draws over ALL of this person's finished games.
    Task<GetGameStatsDto> GetStatsAsync(Guid userId);
}
