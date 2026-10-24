using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class PostHashtag : BaseEntity
{
    public Guid PostId { get; set; }
    public Post? Post { get; set; }

    public Guid HashtagId { get; set; }
    public Hashtag? Hashtag { get; set; }
}
