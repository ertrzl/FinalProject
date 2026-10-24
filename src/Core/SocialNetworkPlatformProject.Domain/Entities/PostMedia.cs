using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class PostMedia : BaseEntity
{
    public Guid PostId { get; set; }
    public Post? Post { get; set; }

    public string Url { get; set; } = string.Empty;
    public PostMediaType MediaType { get; set; } = PostMediaType.Image;
    public int SortOrder { get; set; }
}
