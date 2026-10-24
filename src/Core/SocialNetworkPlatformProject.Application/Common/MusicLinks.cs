namespace SocialNetworkPlatformProject.Application.Common;

public static class MusicLinks
{
    private static readonly string[] AudioHosts = { "itunes.apple.com", "mzstatic.com" };
    private static readonly string[] CoverHosts = { "mzstatic.com" };

    public static bool IsAllowedAudio(string? url) => IsAllowed(url, AudioHosts);
    public static bool IsAllowedCover(string? url) => IsAllowed(url, CoverHosts);

    private static bool IsAllowed(string? url, string[] hosts)
    {
        if (string.IsNullOrWhiteSpace(url) || url.Length > 500)
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            return false;

        return hosts.Any(host => uri.Host == host || uri.Host.EndsWith("." + host, StringComparison.OrdinalIgnoreCase));
    }
}
