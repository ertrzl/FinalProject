using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class StoryHighlight : BaseEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;

    public Guid? CoverItemId { get; set; }

    public ICollection<StoryHighlightItem> Items { get; set; } = new List<StoryHighlightItem>();
}
