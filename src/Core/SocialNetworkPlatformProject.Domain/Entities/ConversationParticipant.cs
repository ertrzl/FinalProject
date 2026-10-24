using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class ConversationParticipant : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Conversation? Conversation { get; set; }

    public Guid UserId { get; set; }

    public DateTime? LastReadAt { get; set; }

    public bool IsAdmin { get; set; }
    public bool IsMuted { get; set; }

    public DateTime ReadUpTo => LastReadAt ?? CreatedAt;
}
