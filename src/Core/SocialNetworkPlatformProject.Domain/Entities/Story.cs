using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

// home.html story bar + fullscreen story viewer (js/stories.js). Expires after 24h like the real thing.
public class Story : BaseEntity
{
    public Guid UserId { get; set; }
    public string MediaUrl { get; set; } = string.Empty;
    public StoryMediaType MediaType { get; set; } = StoryMediaType.Image;

    // Optional song (a 30-second preview from Apple's music store, see Application/Common/MusicLinks.cs): it plays while the story is watched.
    public string? MusicTitle { get; set; }
    public string? MusicArtist { get; set; }
    public string? MusicUrl { get; set; }
    public string? MusicArtworkUrl { get; set; }

    // Set on a story somebody added from another person's story ("Hikayene ekle"): whose story it was and which one (soft references,
    // no foreign keys: the original may be deleted while this copy lives its own 24 hours).
    public Guid? ResharedFromStoryId { get; set; }
    public Guid? ResharedFromUserId { get; set; }

    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);
}
