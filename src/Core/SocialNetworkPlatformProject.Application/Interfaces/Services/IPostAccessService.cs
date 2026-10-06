using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// Single place for "who may see this post" so PostService, CommentService and saved posts can't drift apart.
// Rule: author always; friends always; everyone else only if the post is Public and the author's account isn't private.
public interface IPostAccessService
{
    Task<bool> CanViewAsync(Post post, Guid viewerId);

    // Throws NotFoundException (not Forbidden) so the existence of hidden posts isn't leaked.
    Task EnsureCanViewAsync(Post post, Guid viewerId);

    Task<List<Post>> FilterVisibleAsync(IEnumerable<Post> posts, Guid viewerId);

    // The same rules as CanViewAsync, as a database filter: lets a search page through only the posts the viewer
    // may see without loading everyone's posts first. Keep it in step with CanViewAsync/FilterVisibleAsync.
    IQueryable<Post> VisibleTo(IQueryable<Post> posts, Guid viewerId);

    // Who may delete a post (and any comment under it): its author, and on a group post also the group's
    // admins and moderators.
    Task<bool> CanModerateAsync(Post post, Guid userId);

    // CanModerateAsync for a whole page of posts at once: the ids of the posts the user may delete.
    Task<HashSet<Guid>> GetModeratablePostIdsAsync(IReadOnlyCollection<Post> posts, Guid userId);
}
