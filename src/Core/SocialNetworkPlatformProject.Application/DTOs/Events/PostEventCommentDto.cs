namespace SocialNetworkPlatformProject.Application.DTOs.Events;

// event.html's discussion box. IsAnnouncement is organizer-only.
public class PostEventCommentDto
{
    public string Content { get; set; } = string.Empty;
    public bool IsAnnouncement { get; set; }
}
