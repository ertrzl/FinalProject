using System.Globalization;

namespace SocialNetworkPlatformProject.Application.Common;

// The map pin of an event. The form sends the two numbers as text with a dot ("41.0082"): a form field is read in the
// culture of the request (Turkish here, where the dot is the thousands separator), so they are read by hand, always
// with the invariant culture.
public static class EventCoordinates
{
    public const double MaxLatitude = 90;
    public const double MaxLongitude = 180;

    // Nothing sent for either value means "no pin": null, null. Anything sent that is not a number in range is an error
    // (the validator has already turned it away; this is the same rule for the service).
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
