using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// One person has watched one story (js/stories.js: the viewers list only the story's owner can open, and the
// "seen" ring for everybody else). CreatedAt is the moment of the first view. Gone with its story.
public class StoryView : BaseEntity
{
    public Guid StoryId { get; set; }
    public Story? Story { get; set; }

    public Guid ViewerId { get; set; }
}
