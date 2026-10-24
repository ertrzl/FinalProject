using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class StoryLike : BaseEntity
{
    public Guid StoryId { get; set; }
    public Story? Story { get; set; }

    public Guid UserId { get; set; }
}
