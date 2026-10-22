using Microsoft.AspNetCore.Identity;

namespace SocialNetworkPlatformProject.Persistence.Identity;

// Lives in Persistence (not Domain) because it depends on ASP.NET Core Identity.
// Domain entities reference users only by a plain Guid (AuthorId, SenderId, etc.),
// never by a navigation property to this class — that keeps Domain free of
// Persistence dependencies (Onion architecture rule).
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

    // Whether other people can see this user's friend list on the profile (the friend count is always shown).
    // On by default (people expect to find friends of friends); the owner can close it in settings.
    public bool ShowFriendsList { get; set; } = true;

    // settings.html "Bildirim Tercihleri": which kinds of notification are created for this user at all
    // (see NotificationPreferences for which notification types belong to which switch).
    public bool NotifyOnLikes { get; set; } = true;
    public bool NotifyOnComments { get; set; } = true;
    public bool NotifyOnFriendRequests { get; set; } = true;
    public bool NotifyOnStoryTags { get; set; } = true;

    // Drives the green "Çevrimiçi" dot shown on profile.html / navbar avatar
    public DateTime? LastSeenAt { get; set; }

    // profile.html "Katılma tarihi"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Admin panel: a suspended account cannot sign in and its sessions are ended (see AdminService.SuspendUserAsync).
    public bool IsSuspended { get; set; } = false;
    public DateTime? SuspendedAt { get; set; }
    public string? SuspendReason { get; set; }
}
