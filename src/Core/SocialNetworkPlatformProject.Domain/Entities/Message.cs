using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class Message : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Conversation? Conversation { get; set; }

    public Guid SenderId { get; set; }
    public MessageType Type { get; set; } = MessageType.Text;
    public string Text { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public bool IsRead { get; set; } = false;

    public Guid? ReplyToStoryId { get; set; }

    public Guid? ReplyToMessageId { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted => DeletedAt != null;

    public void MarkDeleted()
    {
        DeletedAt = DateTime.UtcNow;
        Text = string.Empty;
        MediaUrl = null;
        ReplyToStoryId = null;
        ReplyToMessageId = null;
    }
}
