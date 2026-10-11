using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

// One photo or video of a post. A post has up to PostLimits.MaxImages photos, or a single video; SortOrder is the
// order the author picked them in (0 = the first one shown).
public class PostMedia : BaseEntity
{
    public Guid PostId { get; set; }
    public Post? Post { get; set; }

    public string Url { get; set; } = string.Empty;
    public PostMediaType MediaType { get; set; } = PostMediaType.Image;
    public int SortOrder { get; set; }
}
