using SocialNetworkPlatformProject.Application.DTOs.Highlights;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IStoryHighlightService
{
    Task<List<GetHighlightDto>> GetForUserAsync(Guid viewerId, Guid userId, Guid? storyId = null);

    Task<GetHighlightDetailDto> GetAsync(Guid viewerId, Guid highlightId);

    Task<GetHighlightDetailDto> CreateAsync(Guid currentUserId, PostHighlightDto dto);

    Task<GetHighlightDetailDto> AddStoryAsync(Guid currentUserId, Guid highlightId, Guid storyId);

    Task<GetHighlightDetailDto> UpdateAsync(Guid currentUserId, Guid highlightId, PutHighlightDto dto);

    Task RemoveItemAsync(Guid currentUserId, Guid highlightId, Guid itemId);

    Task DeleteAsync(Guid currentUserId, Guid highlightId);

    Task<bool> CanViewFileAsync(Guid viewerId, string url);
}
