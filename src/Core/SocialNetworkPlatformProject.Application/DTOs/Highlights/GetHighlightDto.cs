namespace SocialNetworkPlatformProject.Application.DTOs.Highlights;

// One round cover in the row under the profile header (profile.html), and one line of the "Öne çıkanlara ekle" list in the story viewer.
public class GetHighlightDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int ItemCount { get; set; }

    // The picture or video on the cover: the item the owner chose, else the first one.
    public string? CoverUrl { get; set; }
    public string CoverMediaType { get; set; } = "Image"; // "Image" / "Video"

    // Only when the owner's list was asked for with a story id: that story is already kept in this highlight.
    public bool ContainsStory { get; set; }
}
