namespace SocialNetworkPlatformProject.Application.Common;

public static class UtcDateTimeExtensions
{
    // Model binding turns "2026-10-06T15:00:00Z" into a *local* DateTime, JSON bodies keep it UTC, and a value sent
    // without any offset arrives as Unspecified. Everything is stored and compared as UTC, so normalize at the edge.
    // Unspecified is taken to be UTC: the browser always sends an offset, so that only happens for hand-made requests.
    public static DateTime ToUtc(this DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
