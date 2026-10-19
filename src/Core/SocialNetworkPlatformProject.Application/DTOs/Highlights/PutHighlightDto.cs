namespace SocialNetworkPlatformProject.Application.DTOs.Highlights;

// The manage window of a highlight: the new name, and the item to use as the cover (null = the first item).
public class PutHighlightDto
{
    public string Title { get; set; } = string.Empty;
    public Guid? CoverItemId { get; set; }
}
