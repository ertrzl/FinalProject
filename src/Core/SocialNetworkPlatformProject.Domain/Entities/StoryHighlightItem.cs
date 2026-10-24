using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

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

    public Guid SourceStoryId { get; set; }
    public DateTime StoryCreatedAt { get; set; }
}
