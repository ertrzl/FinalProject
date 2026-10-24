using SocialNetworkPlatformProject.Application.DTOs.Posts;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IGroupPostService
{
    Task<List<GetPostDto>> GetPinnedAsync(Guid currentUserId, Guid groupId);

    Task<List<GetPostDto>> PinAsync(Guid currentUserId, Guid groupId, Guid postId);

    Task<List<GetPostDto>> UnpinAsync(Guid currentUserId, Guid groupId, Guid postId);

    Task<PagedResult<GetPostDto>> SearchAsync(Guid currentUserId, Guid groupId, string? term, int page, int pageSize);
}
