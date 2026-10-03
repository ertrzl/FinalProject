using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// events.html cards + "Etkinlik Oluştur" modal
public class Event : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? CoverImageUrl { get; set; }
    public DateTime StartsAt { get; set; }
    public Guid CreatedByUserId { get; set; }

    // A group event is only visible to the group's members. Fixed at creation; deleting the group deletes its events.
    public Guid? GroupId { get; set; }
    public Group? Group { get; set; }

    public int? Capacity { get; set; } // max "Going" attendees; null = unlimited

    public bool IsOnline { get; set; }
    public string? OnlineLink { get; set; } // only revealed to the organizer and people who are going

    // Joining a capped event bumps AttendanceVersion, which touches this row so RowVersion rejects the second of
    // two people racing for the last spot (see EventService.SetStatusAsync).
    public int AttendanceVersion { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<EventAttendee> Attendees { get; set; } = new List<EventAttendee>();
    public ICollection<EventInvite> Invites { get; set; } = new List<EventInvite>();
    public ICollection<EventComment> Comments { get; set; } = new List<EventComment>();
}
