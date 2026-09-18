namespace SocialNetworkPlatformProject.Application.DTOs.Friends;

// "Arkadaş Ekle" buttons (toggleFriendRequest() in app.js) — sent from home/friends/search/profile pages.
public class PostFriendRequestDto
{
    public Guid ReceiverId { get; set; }
}
