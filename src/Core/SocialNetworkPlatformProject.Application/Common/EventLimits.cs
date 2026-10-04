namespace SocialNetworkPlatformProject.Application.Common;

// Business limits for events, shared by the validators and EventService.
public static class EventLimits
{
    public const int MaxCapacity = 100_000;

    // Applied as the end time when the organizer doesn't pick one.
    public static readonly TimeSpan DefaultDuration = TimeSpan.FromHours(3);

    public static readonly TimeSpan MaxDuration = TimeSpan.FromDays(30);
}
