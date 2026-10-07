using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

// events.html cards + "Etkinlik Oluştur" modal
public class Event : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? CoverImageUrl { get; set; }

    // Both are UTC. EndsAt is always stored: when the organizer leaves it out, EventService fills in StartsAt + a
    // default duration, so "has it finished?" is a plain comparison everywhere.
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }

    public Guid CreatedByUserId { get; set; }

    // A group event is only visible to the group's members. Fixed at creation; deleting the group deletes its events.
    public Guid? GroupId { get; set; }
    public Group? Group { get; set; }

    // A private event is invisible to everyone except its organizer, people who attend it and people invited to it
    // (so it never shows up in lists or search for anyone else). Mutually exclusive with GroupId.
    public bool IsPrivate { get; set; }

    public int? Capacity { get; set; } // max "Going" attendees; null = unlimited

    public bool IsOnline { get; set; }
    public string? OnlineLink { get; set; } // only revealed to the organizer and people who are going

    // Anything that changes who holds a spot (joining, leaving, queueing, promoting, a new capacity) bumps
    // AttendanceVersion, which touches this row so RowVersion rejects the second of two people racing for the same
    // spot (see EventService.SetStatusAsync).
    public int AttendanceVersion { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // Reminders go out once each, about a day and about an hour before the start. The timestamps are what stops a
    // reminder from being sent twice (see EventMaintenanceService); null means "not sent yet".
    public DateTime? DayReminderSentAt { get; set; }
    public DateTime? HourReminderSentAt { get; set; }

    public static readonly TimeSpan DayReminderLead = TimeSpan.FromHours(24);
    public static readonly TimeSpan HourReminderLead = TimeSpan.FromHours(1);

    public ICollection<EventAttendee> Attendees { get; set; } = new List<EventAttendee>();
    public ICollection<EventInvite> Invites { get; set; } = new List<EventInvite>();
    public ICollection<EventComment> Comments { get; set; } = new List<EventComment>();

    // Called when the event is created or its start moves. Reminders that are still ahead of us become due again; one
    // whose moment has already passed counts as sent, so an event created for tomorrow morning doesn't immediately
    // announce "starts in a day".
    public void ResetReminders(DateTime now)
    {
        DayReminderSentAt = StartsAt - now <= DayReminderLead ? now : null;
        HourReminderSentAt = StartsAt - now <= HourReminderLead ? now : null;
    }

    // Computed from Attendees, which must be loaded. Not mapped to the database.
    public int GoingCount => Attendees.Count(a => a.Status == EventAttendeeStatus.Going);
    public bool IsFull => IsFullWith(GoingCount);

    // The same rule for a head-count that came from somewhere else (a COUNT query, for a list of events).
    public bool IsFullWith(int goingCount) => Capacity.HasValue && goingCount >= Capacity.Value;

    // Fills every free spot from the waiting list, first come first served, and returns who moved up (none when
    // nothing is free or nobody is waiting). With no capacity at all the whole queue moves up. Only changes the
    // statuses in memory: the caller saves, so the promotion lands in the same transaction as whatever freed the spot.
    public IReadOnlyList<EventAttendee> PromoteWaitlist()
    {
        var freeSpots = Capacity.HasValue ? Capacity.Value - GoingCount : int.MaxValue;
        if (freeSpots <= 0)
            return Array.Empty<EventAttendee>();

        var promoted = Attendees
            .Where(a => a.Status == EventAttendeeStatus.Waitlisted)
            .OrderBy(a => a.WaitlistedAt)
            .Take(freeSpots)
            .ToList();

        foreach (var attendee in promoted)
        {
            attendee.Status = EventAttendeeStatus.Going;
            attendee.WaitlistedAt = null;
        }

        if (promoted.Count > 0)
            AttendanceVersion++;

        return promoted;
    }
}
