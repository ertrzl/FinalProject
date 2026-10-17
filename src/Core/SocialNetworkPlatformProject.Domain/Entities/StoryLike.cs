using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// One person liked one story (the heart in js/stories.js's viewer; the owner sees who liked in the viewers list).
// Gone with its story.
public class StoryLike : BaseEntity
{
    public Guid StoryId { get; set; }
    public Story? Story { get; set; }

    public Guid UserId { get; set; }
}
