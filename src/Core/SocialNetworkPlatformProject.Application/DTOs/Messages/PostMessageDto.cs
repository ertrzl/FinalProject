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

    // Set to answer a story: the message goes to the story's owner and is shown together with the story. Plain text
    // only, and only while the story is still up and the sender may see it (a friend of the owner).
    public Guid? StoryId { get; set; }

    // Set to answer a message of the conversation (WhatsApp-style "reply": the new message shows a quote of that one).
    // It has to be a message of this conversation that has not been taken back.
    public Guid? ReplyToMessageId { get; set; }
}
