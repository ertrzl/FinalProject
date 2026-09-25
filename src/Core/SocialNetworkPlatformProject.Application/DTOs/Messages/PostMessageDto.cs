namespace SocialNetworkPlatformProject.Application.DTOs.Messages;

// Sent through MessagesHub (SignalR) and mirrored via REST for history/fallback.
// Exactly one of ConversationId / ReceiverId is set: ReceiverId starts a new conversation
// (matches messages.html?with=Name and the chat dock's deep-link behavior).
// Type is "Text" (default) or "Sticker" (Text is then the sticker's emoji); images go through PostMessageImageDto.
public class PostMessageDto
{
    public Guid? ConversationId { get; set; }
    public Guid? ReceiverId { get; set; }
    public string? Type { get; set; }
    public string Text { get; set; } = string.Empty;
}
