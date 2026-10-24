using Microsoft.AspNetCore.Http;

namespace SocialNetworkPlatformProject.Application.DTOs.Events;

public abstract class EventInputDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Location { get; set; }

    public string? Latitude { get; set; }
    public string? Longitude { get; set; }

    public DateTime StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }

    public IFormFile? CoverImage { get; set; }

    public bool IsPrivate { get; set; }

    public int? Capacity { get; set; }

    public bool IsOnline { get; set; }
    public string? OnlineLink { get; set; }
}
