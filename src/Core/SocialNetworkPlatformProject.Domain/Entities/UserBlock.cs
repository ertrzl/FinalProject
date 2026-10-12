using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// One person blocking another. The block is one-way to set and undo (only the blocker can lift it), but it hides the
// two people from each other: neither sees the other's posts, profile, stories, or can write to the other.
public class UserBlock : BaseEntity
{
    public Guid BlockerId { get; set; }
    public Guid BlockedId { get; set; }
}
