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

    public bool IsOwnProfile { get; set; }
    public string FriendshipStatus { get; set; } = "None"; // "None" / "Friends" / "RequestSent" / "RequestReceived"
}
