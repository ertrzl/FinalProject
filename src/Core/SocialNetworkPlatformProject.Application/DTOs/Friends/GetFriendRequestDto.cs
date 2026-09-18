namespace SocialNetworkPlatformProject.Application.DTOs.Friends;

// friends.html "Gelen İstekler" / "Gönderilen İstekler" tabs
public class GetFriendRequestDto
{
    public Guid Id { get; set; }

    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string? SenderAvatarUrl { get; set; }

    public Guid ReceiverId { get; set; }
    public string ReceiverName { get; set; } = string.Empty;
    public string? ReceiverAvatarUrl { get; set; }

    public string Status { get; set; } = string.Empty; // "Pending" / "Accepted" / "Declined" / "Cancelled"
    public DateTime CreatedAt { get; set; }
    public int MutualFriendsCount { get; set; }
}
