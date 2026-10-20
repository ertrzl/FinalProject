using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

// groups.html "Katıl" / "Ayrıl" buttons (toggleGroupMembership() in app.js)
public class GroupMember : BaseEntity
{
    public Guid GroupId { get; set; }
    public Group? Group { get; set; }

    public Guid UserId { get; set; }
    public GroupMemberRole Role { get; set; } = GroupMemberRole.Member;

    // "Sessize al": this member gets no notifications (and no chat toasts) from the group's activity. Stored on the
    // membership, so it goes when they leave and a later join starts unmuted.
    public bool IsMuted { get; set; }
}
