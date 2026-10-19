namespace SocialNetworkPlatformProject.Application.DTOs.Highlights;

// One story inside a highlight, shaped like a story so the viewer can play it (the file is the highlight's own copy).
public class GetHighlightItemDto
{
    public Guid Id { get; set; }
    public string MediaUrl { get; set; } = string.Empty;
    public string MediaType { get; set; } = "Image"; // "Image" / "Video"
    public string? MusicTitle { get; set; }
    public string? MusicArtist { get; set; }
    public string? MusicUrl { get; set; }
    public string? MusicArtworkUrl { get; set; }

    // When the story was posted.
    public DateTime CreatedAt { get; set; }
}
