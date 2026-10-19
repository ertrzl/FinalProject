namespace SocialNetworkPlatformProject.Application.DTOs.Posts;

// The event shown as a card inside a post that shares it ("Gönderide paylaş" on event.html).
public class GetSharedEventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public string? Location { get; set; }
    public bool IsOnline { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool IsOngoing { get; set; }
    public bool IsPast { get; set; }
}
