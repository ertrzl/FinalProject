namespace SocialNetworkPlatformProject.Application.DTOs.Messages;

public class GetMessageDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }

    public string? SenderName { get; set; }
    public string? SenderAvatarUrl { get; set; }

    public string Type { get; set; } = "Text";
    public string Text { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public bool IsMine { get; set; }
    public bool IsRead { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime SentAt { get; set; }

    public GetStoryReplyDto? StoryReply { get; set; }

    public GetMessageReplyDto? ReplyTo { get; set; }

    public List<GetMessageReactionDto> Reactions { get; set; } = new();
}
