using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class StoryTag : BaseEntity
{
    public Guid StoryId { get; set; }
    public Story? Story { get; set; }

    public Guid TaggedUserId { get; set; }
}
