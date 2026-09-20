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
}
