using System.Globalization;
using SocialNetworkPlatformProject.Application.DTOs.Events;

namespace SocialNetworkPlatformProject.Application.Common;

public static class GoogleCalendarLinkBuilder
{
    private const string BaseAddress = "https://calendar.google.com/calendar/render";

    public static string Build(GetEventDto ev, string? pageUrl)
    {
        var details = new List<string>();
        if (!string.IsNullOrWhiteSpace(ev.Description)) details.Add(ev.Description);
        if (ev.IsOnline && !string.IsNullOrEmpty(ev.OnlineLink)) details.Add(Loc.T("event.calendar.link", ev.OnlineLink));
        if (!string.IsNullOrEmpty(pageUrl)) details.Add(Loc.T("event.calendar.page", pageUrl));

        var place = ev.IsOnline ? ev.OnlineLink ?? Loc.T("event.calendar.online") : ev.Location ?? string.Empty;

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
