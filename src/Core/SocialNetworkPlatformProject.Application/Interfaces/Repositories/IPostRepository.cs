using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories;

public interface IPostRepository : IRepository<Post>
{
    // Like and comment counts for several posts in two COUNT queries (every id is in the result, 0 when there are none).
    Task<Dictionary<Guid, PostCounts>> GetCountsAsync(IReadOnlyCollection<Guid> postIds);

    // How many times each of these posts was shared (posts nobody shared are left out).
    Task<Dictionary<Guid, int>> GetShareCountsAsync(IReadOnlyCollection<Guid> postIds);

    // The shares of these posts stop pointing at them (SharedPostId becomes null), so they can be deleted.
    // The shares themselves stay and show "this post is gone".
    Task DetachSharesAsync(IReadOnlyCollection<Guid> postIds);

    // Narrows a post query to what this viewer may see, inside the SQL: not the posts of somebody blocked either way; a
    // personal post only for its author, the author's friends, or anyone if it is public and the account is open; a group
    // post for anyone if the group is public, otherwise for its members.
    IQueryable<Post> VisibleTo(IQueryable<Post> posts, Guid viewerId);

    // The post that owns the photo or video with this address; null when there is none.
    Task<Post?> FindByMediaUrlAsync(string url);
}
