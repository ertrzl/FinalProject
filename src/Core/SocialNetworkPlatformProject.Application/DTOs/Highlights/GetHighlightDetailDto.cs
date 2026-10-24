namespace SocialNetworkPlatformProject.Application.DTOs.Highlights;

public class GetHighlightDetailDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;

    public Guid OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string? OwnerAvatarUrl { get; set; }
    public bool IsOwn { get; set; }

    public Guid? CoverItemId { get; set; }
    public Guid? EffectiveCoverItemId { get; set; }

    public List<GetHighlightItemDto> Items { get; set; } = new();
}
