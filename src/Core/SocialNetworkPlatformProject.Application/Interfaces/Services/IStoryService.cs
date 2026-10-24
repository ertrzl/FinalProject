using SocialNetworkPlatformProject.Application.DTOs.Common;
using SocialNetworkPlatformProject.Application.DTOs.Stories;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IStoryService
{
    Task<GetStoryDto> CreateAsync(Guid currentUserId, PostStoryDto dto);

    Task<List<GetStoryDto>> GetActiveStoriesAsync(Guid currentUserId);

    Task<GetStoryDto> ReshareAsync(Guid currentUserId, Guid storyId);

    Task DeleteAsync(Guid currentUserId, Guid storyId);

    Task RecordViewAsync(Guid currentUserId, Guid storyId);

    Task<GetLikeResultDto> ToggleLikeAsync(Guid currentUserId, Guid storyId);

    Task<List<GetStoryViewerDto>> GetViewersAsync(Guid currentUserId, Guid storyId);
}
