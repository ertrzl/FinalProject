using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

// F2: friends.html "Gelen İstekler" / "Gönderilen İstekler" tabs.
// On Accepted, a Friendship row is created and this request is kept for history.
public class FriendRequest : BaseEntity
{
    public Guid SenderId { get; set; }
    public Guid ReceiverId { get; set; }
    public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;
    public DateTime? RespondedAt { get; set; }
}
