using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Common;
using SocialNetworkPlatformProject.Application.DTOs.Posts;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IPostService
{
    Task<GetPostDto> CreateAsync(Guid currentUserId, PostPostDto dto);

    Task<GetPostDto> UpdateAsync(Guid currentUserId, Guid postId, PutPostDto dto);

    Task DeleteAsync(Guid currentUserId, Guid postId);

    Task<GetPostDto> GetByIdAsync(Guid currentUserId, Guid postId);

    // Chronological feed: the user's own posts plus their friends' posts.
    Task<PagedResult<GetPostDto>> GetFeedAsync(Guid currentUserId, int page, int pageSize);

    // profile.html "Gönderiler" tab, filtered by what the viewer is allowed to see.
    Task<PagedResult<GetPostDto>> GetUserPostsAsync(Guid profileUserId, Guid currentUserId, int page, int pageSize);

    Task<GetLikeResultDto> ToggleLikeAsync(Guid currentUserId, Guid postId);

    // Returns true when the post is saved after the toggle.
    Task<bool> ToggleSaveAsync(Guid currentUserId, Guid postId);

    Task<PagedResult<GetPostDto>> GetSavedPostsAsync(Guid currentUserId, int page, int pageSize);
}
