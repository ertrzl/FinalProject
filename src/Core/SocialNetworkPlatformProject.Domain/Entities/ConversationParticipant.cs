using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class ConversationParticipant : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Conversation? Conversation { get; set; }

    public Guid UserId { get; set; }

    // Group chats only: up to when this person has read the chat (one flag per message, Message.IsRead, only works for
    // two people). Null = nothing read yet; the messages from before they joined (CreatedAt) never count as unread.
    public DateTime? LastReadAt { get; set; }

    // Custom group chats only (see Conversation.IsCustomGroup): this person runs the chat; this person muted it (no toasts, left
    // out of the unread total). A chat that belongs to a group (GroupId) keeps its mute switch on GroupMember.IsMuted.
    public bool IsAdmin { get; set; }
    public bool IsMuted { get; set; }

    public DateTime ReadUpTo => LastReadAt ?? CreatedAt;
}
