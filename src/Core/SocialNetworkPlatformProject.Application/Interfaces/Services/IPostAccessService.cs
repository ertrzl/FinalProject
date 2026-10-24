using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IPostAccessService
{
    Task<bool> CanViewAsync(Post post, Guid viewerId);

    Task EnsureCanViewAsync(Post post, Guid viewerId);

    Task<List<Post>> FilterVisibleAsync(IEnumerable<Post> posts, Guid viewerId);

    IQueryable<Post> VisibleTo(IQueryable<Post> posts, Guid viewerId);

    Task<bool> CanModerateAsync(Post post, Guid userId);

    Task<HashSet<Guid>> GetModeratablePostIdsAsync(IReadOnlyCollection<Post> posts, Guid userId);
}
