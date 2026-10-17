namespace SocialNetworkPlatformProject.Application.DTOs.Messages;

// The story a message answers (messages.html / chat dock show it above the text). A story lives 24 hours: once it is
// gone IsAvailable is false and there is no media, the bubble then says the story is no longer there.
public class GetStoryReplyDto
{
    public Guid StoryId { get; set; }
    public bool IsAvailable { get; set; }
    public string? MediaUrl { get; set; }
    public string MediaType { get; set; } = "Image"; // "Image" / "Video"
}
