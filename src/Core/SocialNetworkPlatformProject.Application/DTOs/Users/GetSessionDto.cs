namespace SocialNetworkPlatformProject.Application.DTOs.Users;

public class GetSessionDto
{
    public Guid Id { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public DateTime StartedAt { get; set; }

    public DateTime LastActiveAt { get; set; }

    public bool IsCurrent { get; set; }
}
