namespace SocialNetworkPlatformProject.Application.Common;

// The quick date filters of the events list ("Bugün", "Bu hafta", "Bu ay"). What counts as "today" depends on the
// viewer's time zone, so the browser only sends the zone's name and the boundaries are worked out here.
public static class EventDateRange
{
    public const string Today = "today";
    public const string Week = "week";   // today and the next 7 days
    public const string Month = "month"; // until the end of the current month

    public static readonly IReadOnlyList<string> All = new[] { Today, Week, Month };

    // IANA ("Europe/Istanbul") or Windows names; null/blank means UTC. False when the name isn't a known zone.
    public static bool TryResolveZone(string? timeZoneId, out TimeZoneInfo zone)
    {
        zone = TimeZoneInfo.Utc;
        return string.IsNullOrWhiteSpace(timeZoneId) || TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId.Trim(), out zone!);
    }

    // The first moment (UTC) that no longer belongs to the range: an event counts when it starts before this.
    public static DateTime EndUtc(string range, DateTime nowUtc, TimeZoneInfo zone)
    {
        var localToday = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, zone).Date;

        var localEnd = range switch
        {
            Today => localToday.AddDays(1),
            Week => localToday.AddDays(8),
            Month => new DateTime(localToday.Year, localToday.Month, 1).AddMonths(1),
            _ => throw new ArgumentOutOfRangeException(nameof(range), range, "Unknown date range.")
        };

        // A daylight-saving change can make local midnight not exist; the hour after it is the same instant as "midnight".
        if (zone.IsInvalidTime(localEnd))
            localEnd = localEnd.AddHours(1);

        return TimeZoneInfo.ConvertTimeToUtc(localEnd, zone);
    }
}
