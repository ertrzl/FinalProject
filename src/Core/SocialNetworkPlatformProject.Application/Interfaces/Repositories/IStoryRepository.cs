using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IStoryRepository : IRepository<Story>
{
    Task<List<Story>> GetExpiredAsync(DateTime nowUtc, int take);

    Task<int> DeleteByIdsAsync(IReadOnlyCollection<Guid> ids);
}
