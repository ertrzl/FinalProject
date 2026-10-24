using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Posts;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IPostMentionService
{
    Task SyncAsync(Post post, string? text);

    Task<PagedResult<GetPostDto>> GetTaggedPostsAsync(Guid profileUserId, Guid viewerId, int page, int pageSize);

    Task<int> BackfillAsync();
}
