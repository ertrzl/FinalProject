using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IPostRepository : IRepository<Post>
{
    Task<Dictionary<Guid, PostCounts>> GetCountsAsync(IReadOnlyCollection<Guid> postIds);

    Task<Dictionary<Guid, int>> GetShareCountsAsync(IReadOnlyCollection<Guid> postIds);

    Task DetachSharesAsync(IReadOnlyCollection<Guid> postIds);

    IQueryable<Post> VisibleTo(IQueryable<Post> posts, Guid viewerId);

    Task<Post?> FindByMediaUrlAsync(string url);
}
