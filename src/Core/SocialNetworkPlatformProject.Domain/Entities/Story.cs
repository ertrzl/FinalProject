using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class Story : BaseEntity
{
    public Guid UserId { get; set; }
    public string MediaUrl { get; set; } = string.Empty;
    public StoryMediaType MediaType { get; set; } = StoryMediaType.Image;

    public string? MusicTitle { get; set; }
    public string? MusicArtist { get; set; }
    public string? MusicUrl { get; set; }
    public string? MusicArtworkUrl { get; set; }

    public Guid? ResharedFromStoryId { get; set; }
    public Guid? ResharedFromUserId { get; set; }

    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);
}
