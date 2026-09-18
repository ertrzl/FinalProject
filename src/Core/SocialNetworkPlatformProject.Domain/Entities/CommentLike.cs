using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// F5: "Beğen" on an individual comment (likeComment() in app.js)
public class CommentLike : BaseEntity
{
    public Guid CommentId { get; set; }
    public Comment? Comment { get; set; }

    public Guid UserId { get; set; }
}
