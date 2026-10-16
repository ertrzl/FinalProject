using SocialNetworkPlatformProject.Application.DTOs.Posts;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IPostService
{
    Task<GetPostDto> CreateAsync(Guid currentUserId, PostPostDto dto);

    Task<GetPostDto> UpdateAsync(Guid currentUserId, Guid postId, PutPostDto dto);

    // asAdmin: the site's admin removing it (only the admin panel passes true; the usual rules about whose post it is don't apply).
    Task DeleteAsync(Guid currentUserId, Guid postId, bool asAdmin = false);

    Task<GetPostDto> GetByIdAsync(Guid currentUserId, Guid postId);

    // Chronological feed: the user's own posts plus their friends' posts.
    Task<PagedResult<GetPostDto>> GetFeedAsync(Guid currentUserId, int page, int pageSize);

    // profile.html "Gönderiler" tab, filtered by what the viewer is allowed to see.
    Task<PagedResult<GetPostDto>> GetUserPostsAsync(Guid profileUserId, Guid currentUserId, int page, int pageSize);

    // group.html wall: throws NotFoundException if the group is private and the viewer isn't a member.
    Task<PagedResult<GetPostDto>> GetGroupPostsAsync(Guid groupId, Guid currentUserId, int page, int pageSize);

    // The "Beğen" button: takes any reaction back, or (when there is none) reacts with a plain Like.
    Task<GetPostReactionResultDto> ToggleLikeAsync(Guid currentUserId, Guid postId);

    // The emoji bar: sets the viewer's reaction (changing an earlier one), or removes it when the type is empty.
    // Only a NEW reaction notifies the author; changing it does not notify again.
    Task<GetPostReactionResultDto> SetReactionAsync(Guid currentUserId, Guid postId, string? type);

    // Who reacted, newest first (the latest 100), optionally one reaction only. Visible to whoever sees the post.
    Task<List<GetPostReactorDto>> GetReactionsAsync(Guid currentUserId, Guid postId, string? type);

    // "Gönderiyi paylaş": a new post of the user's that shows the original (sharing a share shares the original).
    // Only publicly visible posts can be shared (not group posts, not friends-only, not private accounts').
    Task<GetPostDto> ShareAsync(Guid currentUserId, Guid postId, PostShareDto dto);

    // Returns true when the post is saved after the toggle.
    Task<bool> ToggleSaveAsync(Guid currentUserId, Guid postId);

    Task<PagedResult<GetPostDto>> GetSavedPostsAsync(Guid currentUserId, int page, int pageSize);

    // search.html "Gönderiler" tab. A term starting with "#" matches a hashtag exactly;
    // otherwise it matches the post text or a hashtag with that exact name.
    Task<PagedResult<GetPostDto>> SearchAsync(Guid currentUserId, string term, int page, int pageSize);

    // explore.html "Popüler gönderiler": other people's posts of the last 7 days that the viewer may see, the most
    // liked and commented first (a comment counts double).
    Task<PagedResult<GetPostDto>> GetPopularPostsAsync(Guid currentUserId, int page, int pageSize);

    // explore.html "Gündemdeki etiketler": the hashtags used most in the last 7 days, counted over the posts the
    // viewer may see.
    Task<List<GetTrendingHashtagDto>> GetTrendingHashtagsAsync(Guid currentUserId, int take);
}
