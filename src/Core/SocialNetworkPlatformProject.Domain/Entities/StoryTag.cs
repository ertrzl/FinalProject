using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// A friend tagged in a story (the story's owner picked them when adding it). The tagged person is told, and may add the story to their own
// ("Hikayene ekle", see StoryService.ReshareAsync). Gone with its story.
public class StoryTag : BaseEntity
{
    public Guid StoryId { get; set; }
    public Story? Story { get; set; }

    public Guid TaggedUserId { get; set; }
}
