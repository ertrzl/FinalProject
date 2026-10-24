namespace SocialNetworkPlatformProject.Application.DTOs.Events;

public class PostEventCommentDto
{
    public string Content { get; set; } = string.Empty;
    public bool IsAnnouncement { get; set; }
}
