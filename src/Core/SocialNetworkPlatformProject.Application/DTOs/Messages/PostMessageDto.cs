namespace SocialNetworkPlatformProject.Application.DTOs.Messages;

public class PostMessageDto
{
    public Guid? ConversationId { get; set; }
    public Guid? ReceiverId { get; set; }
    public string? Type { get; set; }
    public string Text { get; set; } = string.Empty;

    public Guid? StoryId { get; set; }

    public Guid? ReplyToMessageId { get; set; }
}
