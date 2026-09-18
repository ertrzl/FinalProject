using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// A single chat bubble — sent live over SignalR's MessagesHub, persisted here for history.
public class Message : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Conversation? Conversation { get; set; }

    public Guid SenderId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;
}
