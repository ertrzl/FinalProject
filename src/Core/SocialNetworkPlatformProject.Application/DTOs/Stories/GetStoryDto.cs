namespace SocialNetworkPlatformProject.Application.DTOs.Stories;

// home.html story bar + fullscreen viewer (js/stories.js)
public class GetStoryDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatarUrl { get; set; }
    public string MediaUrl { get; set; } = string.Empty;
    public string MediaType { get; set; } = "Image"; // "Image" / "Video"
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    // Only filled in on your own stories: how many people have watched it (the list is GET /api/stories/{id}/viewers).
    public int ViewCount { get; set; }

    // Somebody else's story that you have already watched: its ring on the story bar is greyed out.
    public bool IsViewed { get; set; }
}
