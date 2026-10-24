namespace SocialNetworkPlatformProject.Application.DTOs.Highlights;

public class GetHighlightItemDto
{
    public Guid Id { get; set; }
    public string MediaUrl { get; set; } = string.Empty;
    public string MediaType { get; set; } = "Image";
    public string? MusicTitle { get; set; }
    public string? MusicArtist { get; set; }
    public string? MusicUrl { get; set; }
    public string? MusicArtworkUrl { get; set; }

    public DateTime CreatedAt { get; set; }
}
