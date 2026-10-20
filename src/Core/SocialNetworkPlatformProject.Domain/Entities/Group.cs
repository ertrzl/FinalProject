using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

// groups.html "Gruplarım" / "Keşfet" tabs, create-group modal
public class Group : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? CoverImageUrl { get; set; }

    // The small round picture of the group (lists, the header of group.html, the group chat), apart from the wide cover.
    public string? AvatarUrl { get; set; }

    public GroupPrivacy Privacy { get; set; } = GroupPrivacy.Public;
    public Guid CreatedByUserId { get; set; }

    public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
    public ICollection<GroupJoinRequest> JoinRequests { get; set; } = new List<GroupJoinRequest>();
    public ICollection<GroupInvite> Invites { get; set; } = new List<GroupInvite>();
    public ICollection<GroupRule> Rules { get; set; } = new List<GroupRule>();
}
