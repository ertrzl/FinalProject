using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class PostMention : BaseEntity
{
    public Guid PostId { get; set; }
    public Post? Post { get; set; }

    public Guid MentionedUserId { get; set; }
}
