namespace SocialNetworkPlatformProject.Application.DTOs.Highlights;

public class PutHighlightDto
{
    public string Title { get; set; } = string.Empty;
    public Guid? CoverItemId { get; set; }
}
