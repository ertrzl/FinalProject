using System.Globalization;

namespace SocialNetworkPlatformProject.Application.Common;

public static class EventCoordinates
{
    public const double MaxLatitude = 90;
    public const double MaxLongitude = 180;

    public static bool TryParse(string? latitude, string? longitude, out double? parsedLatitude, out double? parsedLongitude)
    {
        parsedLatitude = null;
        parsedLongitude = null;

        var hasLatitude = !string.IsNullOrWhiteSpace(latitude);
        var hasLongitude = !string.IsNullOrWhiteSpace(longitude);
        if (!hasLatitude && !hasLongitude)
            return true;

        if (!hasLatitude || !hasLongitude)
            return false;

        if (!TryParseOne(latitude!, MaxLatitude, out var lat) || !TryParseOne(longitude!, MaxLongitude, out var lng))
            return false;

        parsedLatitude = lat;
        parsedLongitude = lng;
        return true;
    }

    private static bool TryParseOne(string text, double limit, out double value)
    {
        return double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
            && double.IsFinite(value)
            && value >= -limit && value <= limit;
    }
}
