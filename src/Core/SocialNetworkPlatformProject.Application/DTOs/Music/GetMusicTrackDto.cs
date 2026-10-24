namespace SocialNetworkPlatformProject.Application.DTOs.Music;

public class GetMusicTrackDto
{
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string PreviewUrl { get; set; } = string.Empty;
    public string? ArtworkUrl { get; set; }
}
