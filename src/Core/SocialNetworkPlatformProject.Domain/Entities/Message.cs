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

    // The story this message answers (soft reference, no FK): the story is gone after 24 hours, the message stays and
    // then shows that its story is no longer there.
    public Guid? ReplyToStoryId { get; set; }

    // The message of the same conversation this one answers (soft reference, no FK). The quote is read from that message
    // when this one is shown, so it follows what happens to it (taken back: "this message was deleted").
    public Guid? ReplyToMessageId { get; set; }

    // "Delete for everyone": the sender took the message back. The row stays (so the conversation keeps its shape and
    // both people see "this message was deleted"), the content does not.
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
