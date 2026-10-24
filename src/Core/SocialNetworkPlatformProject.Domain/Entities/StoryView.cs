using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class StoryView : BaseEntity
{
    public Guid StoryId { get; set; }
    public Story? Story { get; set; }

    public Guid ViewerId { get; set; }
}
