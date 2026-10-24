using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class PostLike : BaseEntity
{
    public Guid PostId { get; set; }
    public Post? Post { get; set; }

    public Guid UserId { get; set; }

    public ReactionType Type { get; set; } = ReactionType.Like;
}
