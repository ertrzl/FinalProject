using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class Event : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public string? CoverImageUrl { get; set; }

    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }

    public Guid CreatedByUserId { get; set; }

    public Guid? GroupId { get; set; }
    public Group? Group { get; set; }

    public bool IsPrivate { get; set; }

    public int? Capacity { get; set; }

    public bool IsOnline { get; set; }
    public string? OnlineLink { get; set; }

    public int AttendanceVersion { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public DateTime? DayReminderSentAt { get; set; }
    public DateTime? HourReminderSentAt { get; set; }

    public static readonly TimeSpan DayReminderLead = TimeSpan.FromHours(24);
    public static readonly TimeSpan HourReminderLead = TimeSpan.FromHours(1);

    public ICollection<EventAttendee> Attendees { get; set; } = new List<EventAttendee>();
    public ICollection<EventInvite> Invites { get; set; } = new List<EventInvite>();
    public ICollection<EventComment> Comments { get; set; } = new List<EventComment>();
    public ICollection<EventPhoto> Photos { get; set; } = new List<EventPhoto>();

    public void ResetReminders(DateTime now)
    {
        DayReminderSentAt = StartsAt - now <= DayReminderLead ? now : null;
        HourReminderSentAt = StartsAt - now <= HourReminderLead ? now : null;
    }

    public int GoingCount => Attendees.Count(a => a.Status == EventAttendeeStatus.Going);
    public bool IsFull => IsFullWith(GoingCount);

    public bool IsFullWith(int goingCount) => Capacity.HasValue && goingCount >= Capacity.Value;

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
