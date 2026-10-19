namespace SocialNetworkPlatformProject.Application.DTOs.Highlights;

// A highlight with its stories: what the viewer plays and what the owner's manage window edits.
public class GetHighlightDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;

    public Guid OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string? OwnerAvatarUrl { get; set; }
    public bool IsOwn { get; set; }

    // The item chosen as the cover (null while the first one is the cover) and the item that is the cover now.
    public Guid? CoverItemId { get; set; }
    public Guid? EffectiveCoverItemId { get; set; }

    // Oldest story first.
    public List<GetHighlightItemDto> Items { get; set; } = new();
}
