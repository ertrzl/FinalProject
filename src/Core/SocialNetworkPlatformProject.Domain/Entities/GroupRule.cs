using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// One rule of a group (group.html "Kurallar" tab), written by its admins. A group that has rules lets people in only
// after they accepted them (GroupService.JoinAsync / AcceptInviteAsync). Gone with its group.
public class GroupRule : BaseEntity
{
    public Guid GroupId { get; set; }
    public Group? Group { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Text { get; set; }

    // The order the admins put the rules in (0 first).
    public int SortOrder { get; set; }
}
