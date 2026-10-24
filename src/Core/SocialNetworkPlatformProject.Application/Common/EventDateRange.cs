namespace SocialNetworkPlatformProject.Application.Common;

public static class EventDateRange
{
    public const string Today = "today";
    public const string Week = "week";
    public const string Month = "month";

    public static readonly IReadOnlyList<string> All = new[] { Today, Week, Month };

    public static bool TryResolveZone(string? timeZoneId, out TimeZoneInfo zone)
    {
        zone = TimeZoneInfo.Utc;
        return string.IsNullOrWhiteSpace(timeZoneId) || TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId.Trim(), out zone!);
    }

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

        if (zone.IsInvalidTime(localEnd))
            localEnd = localEnd.AddHours(1);

        return TimeZoneInfo.ConvertTimeToUtc(localEnd, zone);
    }
}
