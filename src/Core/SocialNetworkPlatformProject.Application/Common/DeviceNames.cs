namespace SocialNetworkPlatformProject.Application.Common;

// "Mozilla/5.0 (Windows NT 10.0; ...) Chrome/126 ..." -> "Chrome · Windows", for the list of open sessions.
// Only the common browsers and systems are told apart; anything else is shown as what it is not known to be.
public static class DeviceNames
{
    public static string FromUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
            return Loc.T("session.unknownDevice");

        var browser = BrowserOf(userAgent);
        var system = SystemOf(userAgent);

        return (browser, system) switch
        {
            (null, null) => Loc.T("session.unknownDevice"),
            (null, _) => system!,
            (_, null) => browser!,
            _ => $"{browser} · {system}"
        };
    }

    // The order matters: Edge and Opera also say "Chrome", and Chrome also says "Safari".
    private static string? BrowserOf(string ua)
    {
        if (ua.Contains("Edg/") || ua.Contains("EdgA/") || ua.Contains("EdgiOS/")) return "Edge";
        if (ua.Contains("OPR/") || ua.Contains("Opera")) return "Opera";
        if (ua.Contains("Firefox/") || ua.Contains("FxiOS/")) return "Firefox";
        if (ua.Contains("Chrome/") || ua.Contains("CriOS/")) return "Chrome";
        if (ua.Contains("Safari/")) return "Safari";
        return null;
    }

    // iPhone and iPad say "like Mac OS X", Android says "Linux": the more specific names are checked first.
    private static string? SystemOf(string ua)
    {
        if (ua.Contains("iPhone")) return "iPhone";
        if (ua.Contains("iPad")) return "iPad";
        if (ua.Contains("Android")) return "Android";
        if (ua.Contains("Windows")) return "Windows";
        if (ua.Contains("Mac OS X") || ua.Contains("Macintosh")) return "macOS";
        if (ua.Contains("CrOS")) return "ChromeOS";
        if (ua.Contains("Linux")) return "Linux";
        return null;
    }
}
