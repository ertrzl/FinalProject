using System.Globalization;
using System.Text;
using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Common;

// Builds an iCalendar (RFC 5545) file for one event, so it can be added to Google / Apple / Outlook calendars.
// Pure formatting: no I/O, no clock (the caller passes "now"), so it is easy to reason about.
public static class IcsBuilder
{
    private const string LineBreak = "\r\n";
    private const int MaxLineBytes = 75; // RFC 5545 §3.1: longer lines must be folded

    // The DTO is already what this particular viewer may see, so the online link only appears for the organizer
    // and people who are going. pageUrl (optional) is the event's own page.
    public static string Build(GetEventDto ev, string? pageUrl, DateTime generatedAtUtc)
    {
        var lines = new List<string>
        {
            "BEGIN:VCALENDAR",
            "VERSION:2.0",
            "PRODID:-//SocialNet//Etkinlikler//TR",
            "CALSCALE:GREGORIAN",
            "METHOD:PUBLISH",
            "BEGIN:VEVENT",
            $"UID:{ev.Id}@socialnet",
            $"DTSTAMP:{FormatUtc(generatedAtUtc)}",
            $"DTSTART:{FormatUtc(ev.StartsAt)}",
            $"DTEND:{FormatUtc(ev.EndsAt)}",
            $"SUMMARY:{Escape(ev.Title)}"
        };

        var place = ev.IsOnline ? "Çevrimiçi" : ev.Location;
        if (!string.IsNullOrWhiteSpace(place))
            lines.Add($"LOCATION:{Escape(place)}");

        var description = new List<string>();
        if (!string.IsNullOrWhiteSpace(ev.Description)) description.Add(ev.Description);
        if (ev.IsOnline && !string.IsNullOrEmpty(ev.OnlineLink)) description.Add($"Bağlantı: {ev.OnlineLink}");
        if (!string.IsNullOrEmpty(pageUrl)) description.Add($"Etkinlik sayfası: {pageUrl}");
        if (description.Count > 0)
            lines.Add($"DESCRIPTION:{Escape(string.Join("\n", description))}");

        // URL is a URI value, not text: it is written as is.
        var url = ev.IsOnline && !string.IsNullOrEmpty(ev.OnlineLink) ? ev.OnlineLink : pageUrl;
        if (!string.IsNullOrEmpty(url))
            lines.Add($"URL:{url}");

        lines.Add("STATUS:CONFIRMED");
        lines.Add("END:VEVENT");
        lines.Add("END:VCALENDAR");

        return string.Concat(lines.Select(line => Fold(line) + LineBreak));
    }

    // "Sahil Voleybolu!" -> "sahil-voleybolu.ics": ASCII only, so the download name survives every browser and OS.
    public static string BuildFileName(string title)
    {
        var ascii = new StringBuilder();
        foreach (var c in title.Trim().ToLowerInvariant())
        {
            ascii.Append(c switch
            {
                // ToLowerInvariant leaves the dotted capital İ alone, so it is mapped here with the lowercase letters.
                'ç' => 'c', 'ğ' => 'g', 'ı' => 'i', 'İ' => 'i', 'ö' => 'o', 'ş' => 's', 'ü' => 'u',
                >= 'a' and <= 'z' => c,
                >= '0' and <= '9' => c,
                _ => '-'
            });
        }

        var slug = string.Join("-", ascii.ToString().Split('-', StringSplitOptions.RemoveEmptyEntries));
        if (slug.Length > 60) slug = slug[..60].TrimEnd('-');
        return (slug.Length == 0 ? "etkinlik" : slug) + ".ics";
    }

    private static string FormatUtc(DateTime value) =>
        value.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);

    // TEXT values: backslash first, then the separators and newlines.
    private static string Escape(string text) => text
        .Replace("\\", "\\\\")
        .Replace(";", "\\;")
        .Replace(",", "\\,")
        .Replace("\r\n", "\\n")
        .Replace("\n", "\\n")
        .Replace("\r", "\\n");

    // Lines are limited to 75 *bytes*, and Turkish letters take two, so count UTF-8 bytes (never splitting a character).
    // Each continuation line starts with one space, which counts toward its 75 bytes.
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
