using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// One person's emoji on one message (messages.html / chat dock: the small pills under a bubble). A person has at most one
// reaction per message: choosing another emoji replaces it. Gone with its message.
public class MessageReaction : BaseEntity
{
    public Guid MessageId { get; set; }
    public Message? Message { get; set; }

    public Guid UserId { get; set; }
    public string Emoji { get; set; } = string.Empty;
}
