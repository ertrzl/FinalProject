namespace SocialNetworkPlatformProject.Application.DTOs.Stories;

// The list under the eye icon of your own story (js/stories.js)
public class GetStoryViewerDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime ViewedAt { get; set; }
}
