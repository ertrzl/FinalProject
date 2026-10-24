using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class Hashtag : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public ICollection<PostHashtag> Posts { get; set; } = new List<PostHashtag>();
}
