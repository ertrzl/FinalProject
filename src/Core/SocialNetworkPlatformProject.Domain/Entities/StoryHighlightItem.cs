using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

// One story kept in a highlight. Stories are deleted 24 hours after they were posted (together with their files), so the item has a
// copy of the file of its own (folder "highlights") and of the song details. Gone with its highlight.
public class StoryHighlightItem : BaseEntity
{
    public Guid HighlightId { get; set; }
    public StoryHighlight? Highlight { get; set; }

    public string MediaUrl { get; set; } = string.Empty;
    public StoryMediaType MediaType { get; set; } = StoryMediaType.Image;

    public string? MusicTitle { get; set; }
    public string? MusicArtist { get; set; }
    public string? MusicUrl { get; set; }
    public string? MusicArtworkUrl { get; set; }

    // The story this was made from (a soft reference: that story is gone soon) and when it was posted. The same story is kept in a
    // highlight once, and the items play oldest first.
    public Guid SourceStoryId { get; set; }
    public DateTime StoryCreatedAt { get; set; }
}
