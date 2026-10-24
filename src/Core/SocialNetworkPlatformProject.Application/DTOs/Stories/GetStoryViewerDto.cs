namespace SocialNetworkPlatformProject.Application.DTOs.Stories;

public class GetStoryViewerDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime ViewedAt { get; set; }

    public bool HasLiked { get; set; }
}
