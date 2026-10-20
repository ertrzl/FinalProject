namespace SocialNetworkPlatformProject.Application.DTOs.Messages;

public class GetMessageDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }

    // Group chats only (a direct chat has two people, the header already names the other one): who wrote it.
    public string? SenderName { get; set; }
    public string? SenderAvatarUrl { get; set; }

    public string Type { get; set; } = "Text"; // Text / Image / Sticker
    public string Text { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public bool IsMine { get; set; }
    public bool IsRead { get; set; }
    public bool IsDeleted { get; set; } // taken back by the sender: Text and MediaUrl are empty
    public DateTime SentAt { get; set; }

    // Set when this message is an answer to somebody's story (see PostMessageDto.StoryId).
    public GetStoryReplyDto? StoryReply { get; set; }

    // Set when this message answers another message of the conversation (see PostMessageDto.ReplyToMessageId).
    public GetMessageReplyDto? ReplyTo { get; set; }

    // The emojis left on this message, with how many people left each (none: empty).
    public List<GetMessageReactionDto> Reactions { get; set; } = new();
}
