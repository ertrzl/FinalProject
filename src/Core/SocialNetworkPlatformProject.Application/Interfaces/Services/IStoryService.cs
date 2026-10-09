using SocialNetworkPlatformProject.Application.DTOs.Stories;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IStoryService
{
    Task<GetStoryDto> CreateAsync(Guid currentUserId, PostStoryDto dto);

    // Non-expired stories from the user and their friends: the ones the user has not watched yet first, then the
    // watched ones, newest first within each group.
    Task<List<GetStoryDto>> GetActiveStoriesAsync(Guid currentUserId);

    Task DeleteAsync(Guid currentUserId, Guid storyId);

    // The user has watched this story. Counted once per person; your own stories are not counted; a story the user
    // may not see is "not found". Calling it again changes nothing.
    Task RecordViewAsync(Guid currentUserId, Guid storyId);

    // Who watched one of your own stories, newest first (the latest 200).
    Task<List<GetStoryViewerDto>> GetViewersAsync(Guid currentUserId, Guid storyId);
}
