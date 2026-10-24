namespace SocialNetworkPlatformProject.Application.Common;

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

    private static string? BrowserOf(string ua)
    {
        if (ua.Contains("Edg/") || ua.Contains("EdgA/") || ua.Contains("EdgiOS/")) return "Edge";
        if (ua.Contains("OPR/") || ua.Contains("Opera")) return "Opera";
        if (ua.Contains("Firefox/") || ua.Contains("FxiOS/")) return "Firefox";
        if (ua.Contains("Chrome/") || ua.Contains("CriOS/")) return "Chrome";
        if (ua.Contains("Safari/")) return "Safari";
        return null;
    }

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
