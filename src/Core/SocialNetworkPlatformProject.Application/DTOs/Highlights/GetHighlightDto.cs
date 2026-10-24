namespace SocialNetworkPlatformProject.Application.DTOs.Highlights;

public class GetHighlightDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int ItemCount { get; set; }

    public string? CoverUrl { get; set; }
    public string CoverMediaType { get; set; } = "Image";

    public bool ContainsStory { get; set; }
}
