namespace SocialNetworkPlatformProject.Application.DTOs.Users;

// profile.html — covers both "my own profile" and "someone else's profile" (?name= today, ?id= once wired to the API).
// IsOwnProfile drives whether the frontend shows "Profili Düzenle" or "Arkadaş Ekle / Mesaj Gönder".
public class GetUserProfileDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? CoverPhotoUrl { get; set; }

    public string? Bio { get; set; }
    public string? Location { get; set; }
    public string? Occupation { get; set; }
    public string? Education { get; set; }
    public DateTime JoinedAt { get; set; }

    public int FriendCount { get; set; }
    public bool IsOnline { get; set; }
    public bool IsPrivateAccount { get; set; }

    // The owner's switches (settings.html "Gizlilik" loads them); ShowFriendsList also tells a visitor whether the
    // friend list can be opened (GET /api/friends/of/{id}).
    public bool ShowOnlineStatus { get; set; }
    public bool ShowFriendsList { get; set; }

    public bool IsOwnProfile { get; set; }

    // The current user blocked this person: the page offers "Engeli Kaldır" instead of the friend / message buttons.
    public bool IsBlockedByMe { get; set; }
    public string FriendshipStatus { get; set; } = "None"; // "None" / "Friends" / "RequestSent" / "RequestReceived"

    // Whether the "Mesaj Gönder" button should be shown; the server applies the messaging rule (see IMessageService.CanMessageAsync).
    public bool CanMessage { get; set; }
}
