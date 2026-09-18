using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// F5: "Beğen" button on posts (toggleLike() in app.js)
public class PostLike : BaseEntity
{
    public Guid PostId { get; set; }
    public Post? Post { get; set; }

    public Guid UserId { get; set; }
}
