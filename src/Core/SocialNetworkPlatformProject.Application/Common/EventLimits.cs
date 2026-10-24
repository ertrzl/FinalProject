namespace SocialNetworkPlatformProject.Application.Common;

public static class EventLimits
{
    public const int MaxCapacity = 100_000;

    public static readonly TimeSpan DefaultDuration = TimeSpan.FromHours(3);

    public static readonly TimeSpan MaxDuration = TimeSpan.FromDays(30);

    public const int MaxPhotosPerPerson = 10;
    public const int MaxPhotosPerEvent = 200;

    public static readonly TimeSpan CheckInGrace = TimeSpan.FromDays(7);
}
