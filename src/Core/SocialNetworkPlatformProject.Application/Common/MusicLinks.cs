namespace SocialNetworkPlatformProject.Application.Common;

// Where a story's music may come from. Only the preview clips and covers of Apple's music store are accepted: the page plays
// whatever address the story holds, so an address typed by somebody else (a tracking pixel, a huge file) must never get in.
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
