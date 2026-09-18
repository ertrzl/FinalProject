namespace SocialNetworkPlatformProject.Application.DTOs.Stories;

// home.html story bar + fullscreen viewer (js/stories.js)
public class GetStoryDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatarUrl { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
