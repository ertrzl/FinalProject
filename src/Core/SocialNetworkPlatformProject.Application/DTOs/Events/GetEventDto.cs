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
    public Guid CreatedByUserId { get; set; }
    public int? Capacity { get; set; }  // null = no limit
    public bool IsFull { get; set; }    // capped and every spot taken
    public int? SpotsLeft { get; set; } // null when there's no limit

    public bool IsOnline { get; set; }
    public string? OnlineLink { get; set; } // null unless you're the organizer or going

    public Guid? GroupId { get; set; }
    public string? GroupName { get; set; } // set when GroupId is

    public string? InvitedByName { get; set; } // single-event responses: who invited the current user (pending invite)

    public string? CreatedByName { get; set; } // filled for single-event responses only (the detail page), not for lists
    public string? CreatedByAvatarUrl { get; set; }
    public bool IsPast { get; set; } // already started: no more joining, no more editing
    public bool IsOwner { get; set; } // lets the card show "Düzenle" / "Sil" only to the creator

    public int GoingCount { get; set; }
    public int InterestedCount { get; set; }
    public string CurrentUserStatus { get; set; } = "None"; // "None" / "Going" / "Interested"
}
