namespace SocialNetworkPlatformProject.Application.DTOs.Events;

// event.html "Yoklama" (organizer only): the people who are going, each with a tick for "Geldi".
public class GetEventAttendanceDto
{
    // The list can be changed from the start of the event until 7 days after its end; outside that time it is shown
    // read-only. State says which side of that time we are on: "NotYet" (the event has not started), "Open" or "Closed".
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
