using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

// groups.html "Gruplarım" / "Keşfet" tabs, create-group modal
public class Group : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public GroupPrivacy Privacy { get; set; } = GroupPrivacy.Public;
    public Guid CreatedByUserId { get; set; }

    public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
}
