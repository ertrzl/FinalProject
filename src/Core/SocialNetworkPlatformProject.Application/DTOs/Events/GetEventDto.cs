namespace SocialNetworkPlatformProject.Application.DTOs.Events;

// events.html cards
public class GetEventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? CoverImageUrl { get; set; }
    public DateTime StartsAt { get; set; }

    public int GoingCount { get; set; }
    public int InterestedCount { get; set; }
    public string CurrentUserStatus { get; set; } = "None"; // "None" / "Going" / "Interested"
}
