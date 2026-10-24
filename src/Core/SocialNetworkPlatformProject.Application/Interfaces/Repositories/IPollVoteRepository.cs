using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IPollVoteRepository : IRepository<PollVote>
{
    Task<Dictionary<Guid, int>> GetCountsByOptionAsync(IReadOnlyCollection<Guid> pollIds);

    Task<Dictionary<Guid, Guid>> GetMineAsync(Guid userId, IReadOnlyCollection<Guid> pollIds);
}
