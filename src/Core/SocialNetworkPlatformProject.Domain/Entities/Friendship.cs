using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// The established, symmetric relationship once a FriendRequest is accepted.
// friends.html "Arkadaşlarım" tab, profile.html friend grid, "312 arkadaş" counter.
public class Friendship : BaseEntity
{
    public Guid UserOneId { get; set; }
    public Guid UserTwoId { get; set; }
}
