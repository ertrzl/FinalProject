using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// Links a Post to a Hashtag found in its text (many-to-many join, same pattern as PostLike/GroupMember).
public class PostHashtag : BaseEntity
{
    public Guid PostId { get; set; }
    public Post? Post { get; set; }

    public Guid HashtagId { get; set; }
    public Hashtag? Hashtag { get; set; }
}
