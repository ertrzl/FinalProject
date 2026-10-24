namespace SocialNetworkPlatformProject.Application.DTOs.Events;

public class GetEventAttendeeDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsOrganizer { get; set; }
}
