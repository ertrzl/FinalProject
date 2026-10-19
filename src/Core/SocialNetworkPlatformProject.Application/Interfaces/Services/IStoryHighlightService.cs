using SocialNetworkPlatformProject.Application.DTOs.Highlights;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// Story highlights: stories the owner keeps on their profile after the 24 hours are over (each with a copy of its own file).
// Who sees a person's highlights: the person, their friends, and everybody else only while the account is not private; people who
// blocked each other see nothing. The pages never get a highlight they may not see ("not found").
public interface IStoryHighlightService
{
    // The covers on somebody's profile, oldest first; empty when the viewer may not see them. With a story id (your own list only),
    // each cover says whether that story is already in it.
    Task<List<GetHighlightDto>> GetForUserAsync(Guid viewerId, Guid userId, Guid? storyId = null);

    Task<GetHighlightDetailDto> GetAsync(Guid viewerId, Guid highlightId);

    // A new highlight that starts with one of your own running stories.
    Task<GetHighlightDetailDto> CreateAsync(Guid currentUserId, PostHighlightDto dto);

    // Keeps one more of your own running stories in the highlight (a copy of its file; the same story only once).
    Task<GetHighlightDetailDto> AddStoryAsync(Guid currentUserId, Guid highlightId, Guid storyId);

    // Renames and/or changes the cover.
    Task<GetHighlightDetailDto> UpdateAsync(Guid currentUserId, Guid highlightId, PutHighlightDto dto);

    // Takes one story out (its file goes); a highlight left with no stories is deleted.
    Task RemoveItemAsync(Guid currentUserId, Guid highlightId, Guid itemId);

    Task DeleteAsync(Guid currentUserId, Guid highlightId);

    // UploadsController: may this viewer open the file with this address (/uploads/highlights/...)? The same rule as for the highlight.
    Task<bool> CanViewFileAsync(Guid viewerId, string url);
}
