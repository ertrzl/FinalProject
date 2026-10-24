using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class GroupRule : BaseEntity
{
    public Guid GroupId { get; set; }
    public Group? Group { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Text { get; set; }

    public int SortOrder { get; set; }
}
