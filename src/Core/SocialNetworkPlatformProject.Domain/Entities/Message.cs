using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

// A single chat bubble — sent live over SignalR's MessagesHub, persisted here for history.
public class Message : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Conversation? Conversation { get; set; }

    public Guid SenderId { get; set; }
    public MessageType Type { get; set; } = MessageType.Text;
    public string Text { get; set; } = string.Empty;
    public string? MediaUrl { get; set; }
    public bool IsRead { get; set; } = false;

    // "Delete for everyone": the sender took the message back. The row stays (so the conversation keeps its shape and
    // both people see "this message was deleted"), the content does not.
    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted => DeletedAt != null;

    public void MarkDeleted()
    {
        DeletedAt = DateTime.UtcNow;
        Text = string.Empty;
        MediaUrl = null;
    }
}
