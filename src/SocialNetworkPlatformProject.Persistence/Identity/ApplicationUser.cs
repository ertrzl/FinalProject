using Microsoft.AspNetCore.Identity;

namespace SocialNetworkPlatformProject.Persistence.Identity;

// Lives in Infrastructure (not Domain) because it depends on ASP.NET Core Identity.
// Domain entities reference users only by a plain Guid (AuthorId, SenderId, etc.),
// never by a navigation property to this class — that keeps Domain free of
// Infrastructure dependencies (Onion architecture rule).
public class ApplicationUser : IdentityUser<Guid>
{
    // F1: registration fields (register.html)
    public string FullName { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }

    // F6: profile page fields (profile.html "Hakkında" tab)
    public string? Bio { get; set; }
    public string? Location { get; set; }
    public string? Occupation { get; set; }
    public string? Education { get; set; }

    // profile.html avatar-lg / profile-cover
    public string? AvatarUrl { get; set; }
    public string? CoverPhotoUrl { get; set; }

    // settings.html "Gizlilik" section
    public bool IsPrivateAccount { get; set; } = false;
    public bool ShowOnlineStatus { get; set; } = true;

    // Drives the green "Çevrimiçi" dot shown on profile.html / navbar avatar
    public DateTime? LastSeenAt { get; set; }

    // profile.html "Katılma tarihi"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
