using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// An admin-sent invite to a friend — groups.html's "Davetler" tab (the invitee's own choice to accept/decline).
// Deleted on acceptance (a GroupMember row is created instead) or on decline.
public class GroupInvite : BaseEntity
{
    public Guid GroupId { get; set; }
    public Group? Group { get; set; }

    public Guid UserId { get; set; } // the invitee
    public Guid InvitedByUserId { get; set; } // the admin who sent it
}
