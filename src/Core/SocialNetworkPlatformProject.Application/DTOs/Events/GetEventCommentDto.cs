namespace SocialNetworkPlatformProject.Application.DTOs.Events;

// event.html's discussion section: announcements first, then comments newest first.
public class GetEventCommentDto
{
    public Guid Id { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorAvatarUrl { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsAnnouncement { get; set; }
    public bool CanDelete { get; set; } // the author, or the event's organizer
    public DateTime CreatedAt { get; set; }
}
