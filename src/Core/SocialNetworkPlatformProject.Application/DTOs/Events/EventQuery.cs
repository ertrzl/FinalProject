namespace SocialNetworkPlatformProject.Application.DTOs.Events;

// Query-string filters of GET /api/events/upcoming (search box and chips on events.html).
public class EventQuery
{
    public string? Search { get; set; } // matches the title, description or place
    public bool? IsOnline { get; set; } // true = only online events, false = only in-person ones
    public DateTime? From { get; set; } // only events that end after this moment (never earlier than now)
    public DateTime? To { get; set; }   // only events that start before this moment
    public string? When { get; set; }   // "today" | "week" | "month" (see EventDateRange), in the viewer's time zone
    public string? TimeZone { get; set; } // the viewer's zone, e.g. "Europe/Istanbul"; what "today" means depends on it. Default UTC
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

// Values of GET /api/events/mine?scope=...
public static class EventMineScope
{
    public const string Created = "created";     // everything I organized, newest first
    public const string Past = "past";           // finished events I was going to / interested in, newest first
    public const string Attending = "attending"; // events I'm going to / interested in that haven't finished, soonest first
}
