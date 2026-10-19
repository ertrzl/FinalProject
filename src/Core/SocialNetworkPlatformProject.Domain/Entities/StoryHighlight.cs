using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// A named collection of the owner's stories that stays on their profile after the stories themselves expire (the round covers under the
// profile header, js/highlights.js). The items hold their own copies of the pictures and videos. UserId is a soft reference (no foreign
// key, like Story.UserId): the account-deletion code removes the rows and their files.
public class StoryHighlight : BaseEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;

    // The item whose picture is the round cover; null means the first item. A soft reference: removing that item makes the first one the cover again.
    public Guid? CoverItemId { get; set; }

    public ICollection<StoryHighlightItem> Items { get; set; } = new List<StoryHighlightItem>();
}
