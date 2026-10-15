namespace SocialNetworkPlatformProject.Application.DTOs.Users;

// One open session (one sign-in on one device) in the settings page's list.
public class GetSessionDto
{
    public Guid Id { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public DateTime StartedAt { get; set; }

    // The last time the session renewed its token (a page that stays open does it every few minutes).
    public DateTime LastActiveAt { get; set; }

    // The session this very request belongs to.
    public bool IsCurrent { get; set; }
}
