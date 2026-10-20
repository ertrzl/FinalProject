using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// A wall post an admin pinned to the top of its group's wall (group.html "Sabitlendi"), at most GroupLimits.MaxPinnedPosts
// per group. Gone with the post (and so with the group, whose posts go with it). No user id is kept: who pinned it only
// matters at the moment of pinning (the notification names them).
public class GroupPinnedPost : BaseEntity
{
    // Plain column, no foreign key: the group's deletion already reaches this row through its post, and SQL Server
    // refuses two cascade paths to one table.
    public Guid GroupId { get; set; }

    public Guid PostId { get; set; }
    public Post? Post { get; set; }
}
