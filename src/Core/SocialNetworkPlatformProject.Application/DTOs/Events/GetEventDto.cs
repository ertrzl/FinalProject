namespace SocialNetworkPlatformProject.Application.DTOs.Events;

public class GetEventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? CoverImageUrl { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }

    public bool IsOngoing { get; set; }
    public bool IsPast { get; set; }

    public bool IsPrivate { get; set; }

    public bool IsOnline { get; set; }
    public string? OnlineLink { get; set; }

    public int? Capacity { get; set; }
    public bool IsFull { get; set; }
    public int? SpotsLeft { get; set; }
    public int WaitlistCount { get; set; }
    public int? MyWaitlistPosition { get; set; }

    public Guid? GroupId { get; set; }
    public string? GroupName { get; set; }

    public Guid CreatedByUserId { get; set; }
    public bool IsOwner { get; set; }
    public string? CreatedByName { get; set; }
    public string? CreatedByAvatarUrl { get; set; }

    public int GoingCount { get; set; }
    public int InterestedCount { get; set; }
    public string CurrentUserStatus { get; set; } = "None";
    public string? InvitedByName { get; set; }

    public bool MyCheckedIn { get; set; }
    public int PhotoCount { get; set; }
}
