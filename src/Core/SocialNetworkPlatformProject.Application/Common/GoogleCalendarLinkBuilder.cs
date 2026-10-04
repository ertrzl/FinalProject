using System.Globalization;
using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Common;

// Builds Google Calendar's pre-filled "create event" address for one event. Like IcsBuilder it is pure formatting, and
// the DTO is already what this viewer may see, so the online link only appears for the organizer and people going.
public static class GoogleCalendarLinkBuilder
{
    private const string BaseAddress = "https://calendar.google.com/calendar/render";

    public static string Build(GetEventDto ev, string? pageUrl)
    {
        var details = new List<string>();
        if (!string.IsNullOrWhiteSpace(ev.Description)) details.Add(ev.Description);
        if (ev.IsOnline && !string.IsNullOrEmpty(ev.OnlineLink)) details.Add($"Bağlantı: {ev.OnlineLink}");
        if (!string.IsNullOrEmpty(pageUrl)) details.Add($"Etkinlik sayfası: {pageUrl}");

        var place = ev.IsOnline ? ev.OnlineLink ?? "Çevrimiçi" : ev.Location ?? string.Empty;

        var parameters = new (string Name, string Value)[]
        {
            ("action", "TEMPLATE"),
            ("text", ev.Title),
            ("dates", $"{FormatUtc(ev.StartsAt)}/{FormatUtc(ev.EndsAt)}"),
            ("details", string.Join("\n", details)),
            ("location", place)
        };

        return BaseAddress + "?" + string.Join("&", parameters.Select(p => $"{p.Name}={Uri.EscapeDataString(p.Value)}"));
    }

    private static string FormatUtc(DateTime value) =>
        value.ToUniversalTime().ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
}
