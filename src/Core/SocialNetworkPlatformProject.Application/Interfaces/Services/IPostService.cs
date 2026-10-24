using SocialNetworkPlatformProject.Application.DTOs.Posts;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IPostService
{
    Task<GetPostDto> CreateAsync(Guid currentUserId, PostPostDto dto);

    Task<GetPostDto> UpdateAsync(Guid currentUserId, Guid postId, PutPostDto dto);

    Task DeleteAsync(Guid currentUserId, Guid postId, bool asAdmin = false);

    Task<GetPostDto> GetByIdAsync(Guid currentUserId, Guid postId);

    Task<PagedResult<GetPostDto>> GetFeedAsync(Guid currentUserId, int page, int pageSize);

    Task<PagedResult<GetPostDto>> GetUserPostsAsync(Guid profileUserId, Guid currentUserId, int page, int pageSize);

    Task<PagedResult<GetPostDto>> GetGroupPostsAsync(Guid groupId, Guid currentUserId, int page, int pageSize);

    Task<GetPostReactionResultDto> ToggleLikeAsync(Guid currentUserId, Guid postId);

    Task<GetPostReactionResultDto> SetReactionAsync(Guid currentUserId, Guid postId, string? type);

    Task<List<GetPostReactorDto>> GetReactionsAsync(Guid currentUserId, Guid postId, string? type);

    Task<GetPostDto> ShareAsync(Guid currentUserId, Guid postId, PostShareDto dto);

    Task<GetPostDto> ShareEventAsync(Guid currentUserId, Guid eventId, PostShareDto dto);

    Task<bool> ToggleSaveAsync(Guid currentUserId, Guid postId);

    Task<PagedResult<GetPostDto>> GetSavedPostsAsync(Guid currentUserId, int page, int pageSize);

    Task<PagedResult<GetPostDto>> SearchAsync(Guid currentUserId, string term, int page, int pageSize);

    Task<PagedResult<GetPostDto>> GetPopularPostsAsync(Guid currentUserId, int page, int pageSize);

    Task<List<GetTrendingHashtagDto>> GetTrendingHashtagsAsync(Guid currentUserId, int take);
}
