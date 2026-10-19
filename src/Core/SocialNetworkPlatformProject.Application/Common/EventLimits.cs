namespace SocialNetworkPlatformProject.Application.Common;

// Business limits for events, shared by the validators and EventService.
public static class EventLimits
{
    public const int MaxCapacity = 100_000;

    // Applied as the end time when the organizer doesn't pick one.
    public static readonly TimeSpan DefaultDuration = TimeSpan.FromHours(3);

    public static readonly TimeSpan MaxDuration = TimeSpan.FromDays(30);

    // The photo album: how many pictures one person may add to one event, and how many an event may hold in all.
    public const int MaxPhotosPerPerson = 10;
    public const int MaxPhotosPerEvent = 200;

    // The attendance list ("Yoklama") can be filled in from the start of the event until this long after its end.
    public static readonly TimeSpan CheckInGrace = TimeSpan.FromDays(7);
}
