using SocialNetworkPlatformProject.Application.DTOs.Posts;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// What group.html does with its wall beyond the plain list (that one is IPostService.GetGroupPostsAsync): the pinned
// announcements at the top, and searching the wall. Everything follows the group's visibility (a private group's wall is
// for its members only) and the blocks between people, like the wall itself.
public interface IGroupPostService
{
    // The pinned posts, the most recently pinned first.
    Task<List<GetPostDto>> GetPinnedAsync(Guid currentUserId, Guid groupId);

    // Admins only: pins a post of this group's wall (at most GroupLimits.MaxPinnedPosts) and tells the members (not the
    // ones who muted the group). Returns the pinned posts as they are now.
    Task<List<GetPostDto>> PinAsync(Guid currentUserId, Guid groupId, Guid postId);

    // Admins only. Returns the pinned posts as they are now.
    Task<List<GetPostDto>> UnpinAsync(Guid currentUserId, Guid groupId, Guid postId);

    // group.html "Grupta ara": the wall's posts whose text contains the term, or with that hashtag ("#kod" matches the tag
    // only), newest first.
    Task<PagedResult<GetPostDto>> SearchAsync(Guid currentUserId, Guid groupId, string? term, int page, int pageSize);
}
