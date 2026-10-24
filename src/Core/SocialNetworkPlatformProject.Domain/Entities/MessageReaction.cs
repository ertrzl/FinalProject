using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class MessageReaction : BaseEntity
{
    public Guid MessageId { get; set; }
    public Message? Message { get; set; }

    public Guid UserId { get; set; }
    public string Emoji { get; set; } = string.Empty;
}
