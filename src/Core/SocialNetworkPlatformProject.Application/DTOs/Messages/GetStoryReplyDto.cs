namespace SocialNetworkPlatformProject.Application.DTOs.Messages;

public class GetStoryReplyDto
{
    public Guid StoryId { get; set; }
    public bool IsAvailable { get; set; }
    public string? MediaUrl { get; set; }
    public string MediaType { get; set; } = "Image";
}
