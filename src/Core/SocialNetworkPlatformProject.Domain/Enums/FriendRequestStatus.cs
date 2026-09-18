namespace SocialNetworkPlatformProject.Domain.Enums;

// Matches friends.html tabs: "Gelen İstekler" (Pending), accepted -> becomes a Friendship, declined -> removed
public enum FriendRequestStatus
{
    Pending = 0,
    Accepted = 1,
    Declined = 2,
    Cancelled = 3
}
