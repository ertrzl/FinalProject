using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// A pending request to join a private group — group.html's admin-only "Bekleyen İstekler" list.
// Deleted on approval (a GroupMember row is created instead) or on rejection.
public class GroupJoinRequest : BaseEntity
{
    public Guid GroupId { get; set; }
    public Group? Group { get; set; }

    public Guid UserId { get; set; }
}
