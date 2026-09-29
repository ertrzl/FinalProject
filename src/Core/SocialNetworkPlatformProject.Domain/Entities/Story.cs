using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

// home.html story bar + fullscreen story viewer (js/stories.js). Expires after 24h like the real thing.
public class Story : BaseEntity
{
    public Guid UserId { get; set; }
    public string MediaUrl { get; set; } = string.Empty;
    public StoryMediaType MediaType { get; set; } = StoryMediaType.Image;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);
}
