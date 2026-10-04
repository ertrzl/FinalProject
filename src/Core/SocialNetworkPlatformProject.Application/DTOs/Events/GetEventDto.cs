namespace SocialNetworkPlatformProject.Application.DTOs.Events;

// events.html cards + event.html detail page
public class GetEventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? CoverImageUrl { get; set; }

    // Both UTC (serialized with a "Z"); the browser shows them in the viewer's time zone.
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }

    // Upcoming (neither flag), ongoing, or finished. Joining stays open while an event is ongoing; only a finished
    // event is closed for joining, inviting, announcements and editing.
    public bool IsOngoing { get; set; }
    public bool IsPast { get; set; } // finished

    public bool IsPrivate { get; set; }

    public bool IsOnline { get; set; }
    public string? OnlineLink { get; set; } // null unless you're the organizer or going

    public int? Capacity { get; set; }  // null = no limit
    public bool IsFull { get; set; }    // capped and every spot taken
    public int? SpotsLeft { get; set; } // null when there's no limit
    public int WaitlistCount { get; set; }
    public int? MyWaitlistPosition { get; set; } // 1 = next in line; null unless the viewer is on the waiting list

    public Guid? GroupId { get; set; }
    public string? GroupName { get; set; } // set when GroupId is

    public Guid CreatedByUserId { get; set; }
    public bool IsOwner { get; set; } // lets the card show "Düzenle" / "Sil" only to the creator
    public string? CreatedByName { get; set; } // filled for single-event responses only (the detail page), not for lists
    public string? CreatedByAvatarUrl { get; set; }

    public int GoingCount { get; set; }
    public int InterestedCount { get; set; }
    public string CurrentUserStatus { get; set; } = "None"; // "None" / "Going" / "Interested"
    public string? InvitedByName { get; set; } // single-event responses: who invited the current user (pending invite)
}
