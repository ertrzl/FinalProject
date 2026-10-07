using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IStoryRepository : IRepository<Story>
{
    // Stories that have expired, oldest first, at most `take` of them.
    Task<List<Story>> GetExpiredAsync(DateTime nowUtc, int take);

    // Deletes these rows in one statement; returns how many of them were still there.
    Task<int> DeleteByIdsAsync(IReadOnlyCollection<Guid> ids);
}
