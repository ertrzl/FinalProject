using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IPollVoteRepository : IRepository<PollVote>
{
    // Votes per option for several polls at once (options nobody chose are left out).
    Task<Dictionary<Guid, int>> GetCountsByOptionAsync(IReadOnlyCollection<Guid> pollIds);

    // The option the user chose in each of these polls (polls they did not vote in are left out).
    Task<Dictionary<Guid, Guid>> GetMineAsync(Guid userId, IReadOnlyCollection<Guid> pollIds);
}
