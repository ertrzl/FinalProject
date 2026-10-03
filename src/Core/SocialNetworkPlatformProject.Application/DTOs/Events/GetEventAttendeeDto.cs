namespace SocialNetworkPlatformProject.Application.DTOs.Events;

// event.html "Katılımcılar" tab: one row per person going to / interested in the event.
public class GetEventAttendeeDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Status { get; set; } = string.Empty; // "Going" / "Interested"
    public bool IsOrganizer { get; set; }
}
