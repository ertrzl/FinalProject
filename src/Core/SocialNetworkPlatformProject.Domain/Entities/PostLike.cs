using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

// F5: "Beğen" button on posts (toggleLike() in app.js) and the emoji reactions next to it. One row per person and
// post; changing a reaction changes Type, it never adds a second row.
public class PostLike : BaseEntity
{
    public Guid PostId { get; set; }
    public Post? Post { get; set; }

    public Guid UserId { get; set; }

    public ReactionType Type { get; set; } = ReactionType.Like;
}
