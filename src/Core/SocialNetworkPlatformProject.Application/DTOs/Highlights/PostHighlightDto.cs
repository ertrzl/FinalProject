namespace SocialNetworkPlatformProject.Application.DTOs.Highlights;

// "Yeni öne çıkan" in the story viewer: a name, and the story it starts with (one of your own, still running).
public class PostHighlightDto
{
    public string Title { get; set; } = string.Empty;
    public Guid StoryId { get; set; }
}
