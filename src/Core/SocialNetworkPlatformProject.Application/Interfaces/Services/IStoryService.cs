using SocialNetworkPlatformProject.Application.DTOs.Stories;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IStoryService
{
    Task<GetStoryDto> CreateAsync(Guid currentUserId, PostStoryDto dto);

    // Non-expired stories from the user and their friends, newest first.
    Task<List<GetStoryDto>> GetActiveStoriesAsync(Guid currentUserId);

    Task DeleteAsync(Guid currentUserId, Guid storyId);
}
