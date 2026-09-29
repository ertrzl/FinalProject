using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// A "#tag" extracted from a post's text (search.html post search + hashtag filtering).
public class Hashtag : BaseEntity
{
    public string Name { get; set; } = string.Empty; // stored lowercase, without the "#"

    public ICollection<PostHashtag> Posts { get; set; } = new List<PostHashtag>();
}
