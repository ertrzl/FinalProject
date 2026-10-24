namespace SocialNetworkPlatformProject.Application.DTOs.Events;

public class EventQuery
{
    public string? Search { get; set; }
    public bool? IsOnline { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? When { get; set; }
    public string? TimeZone { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 12;
}

public static class EventMineScope
{
    public const string Created = "created";
    public const string Past = "past";
    public const string Attending = "attending";
}
