namespace SocialNetworkPlatformProject.Application.DTOs.Events;

public class GetEventAttendanceDto
{
    public bool IsOpen { get; set; }
    public string State { get; set; } = string.Empty;
    public DateTime OpensAt { get; set; }
    public DateTime ClosesAt { get; set; }

    public int GoingCount { get; set; }
    public int CheckedInCount { get; set; }

    public List<GetEventCheckInRowDto> Items { get; set; } = new();
}

public class GetEventCheckInRowDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool CheckedIn { get; set; }
    public DateTime? CheckedInAt { get; set; }
}
