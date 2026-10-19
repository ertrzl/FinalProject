namespace SocialNetworkPlatformProject.Application.DTOs.Music;

// One song found for "Müzik ekle" in the add-story window: a 30-second preview, not the whole song.
public class GetMusicTrackDto
{
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string PreviewUrl { get; set; } = string.Empty;
    public string? ArtworkUrl { get; set; }
}
