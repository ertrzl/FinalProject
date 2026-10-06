using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IPostRepository : IRepository<Post>
{
    // Like and comment counts for several posts in two COUNT queries (every id is in the result, 0 when there are none).
    Task<Dictionary<Guid, PostCounts>> GetCountsAsync(IReadOnlyCollection<Guid> postIds);
}
