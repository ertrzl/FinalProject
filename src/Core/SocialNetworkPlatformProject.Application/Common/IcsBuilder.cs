using System.Globalization;
using System.Text;
using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Common;

public static class IcsBuilder
{
    private const string LineBreak = "\r\n";
    private const int MaxLineBytes = 75;

    public static string Build(GetEventDto ev, string? pageUrl, DateTime generatedAtUtc)
    {
        var lines = new List<string>
        {
            "BEGIN:VCALENDAR",
            "VERSION:2.0",
            "PRODID:-//Stoop//Events//",
            "CALSCALE:GREGORIAN",
            "METHOD:PUBLISH",
            "BEGIN:VEVENT",
            $"UID:{ev.Id}@socialnet",
            $"DTSTAMP:{FormatUtc(generatedAtUtc)}",
            $"DTSTART:{FormatUtc(ev.StartsAt)}",
            $"DTEND:{FormatUtc(ev.EndsAt)}",
            $"SUMMARY:{Escape(ev.Title)}"
        };

        var place = ev.IsOnline ? Loc.T("event.calendar.online") : ev.Location;
        if (!string.IsNullOrWhiteSpace(place))
            lines.Add($"LOCATION:{Escape(place)}");

        var description = new List<string>();
        if (!string.IsNullOrWhiteSpace(ev.Description)) description.Add(ev.Description);
        if (ev.IsOnline && !string.IsNullOrEmpty(ev.OnlineLink)) description.Add(Loc.T("event.calendar.link", ev.OnlineLink));
        if (!string.IsNullOrEmpty(pageUrl)) description.Add(Loc.T("event.calendar.page", pageUrl));
        if (description.Count > 0)
            lines.Add($"DESCRIPTION:{Escape(string.Join("\n", description))}");

        var url = ev.IsOnline && !string.IsNullOrEmpty(ev.OnlineLink) ? ev.OnlineLink : pageUrl;
        if (!string.IsNullOrEmpty(url))
            lines.Add($"URL:{url}");

        lines.Add("STATUS:CONFIRMED");
        lines.Add("END:VEVENT");
        lines.Add("END:VCALENDAR");

        return string.Concat(lines.Select(line => Fold(line) + LineBreak));
    }

    public static string BuildFileName(string title)
    {
        var ascii = new StringBuilder();
        foreach (var c in title.Trim().ToLowerInvariant())
        {
            ascii.Append(c switch
            {
                'ç' => 'c', 'ğ' => 'g', 'ı' => 'i', 'İ' => 'i', 'ö' => 'o', 'ş' => 's', 'ü' => 'u',
                >= 'a' and <= 'z' => c,
                >= '0' and <= '9' => c,
                _ => '-'
            });
        }

        var slug = string.Join("-", ascii.ToString().Split('-', StringSplitOptions.RemoveEmptyEntries));
        if (slug.Length > 60) slug = slug[..60].TrimEnd('-');
        return (slug.Length == 0 ? Loc.T("event.calendar.fileName") : slug) + ".ics";
    }

    private static string FormatUtc(DateTime value) =>
        value.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

    private static string Escape(string text) => text
        .Replace("\\", "\\\\")
        .Replace(";", "\\;")
        .Replace(",", "\\,")
        .Replace("\r\n", "\\n")
        .Replace("\n", "\\n")
        .Replace("\r", "\\n");

    private static string Fold(string line)
    {
        var result = new StringBuilder();
        var bytesInLine = 0;
        foreach (var rune in line.EnumerateRunes())
        {
            var size = rune.Utf8SequenceLength;
            if (bytesInLine + size > MaxLineBytes)
            {
                result.Append(LineBreak).Append(' ');
                bytesInLine = 1;
            }

            result.Append(rune.ToString());
            bytesInLine += size;
        }

        return result.ToString();
    }
}
