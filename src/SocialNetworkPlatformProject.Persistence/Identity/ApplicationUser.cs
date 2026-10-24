using Microsoft.AspNetCore.Identity;

namespace SocialNetworkPlatformProject.Persistence.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }

    public string? Bio { get; set; }
    public string? Location { get; set; }
    public string? Occupation { get; set; }
    public string? Education { get; set; }

    public string? AvatarUrl { get; set; }
    public string? CoverPhotoUrl { get; set; }

    public bool IsPrivateAccount { get; set; } = false;
    public bool ShowOnlineStatus { get; set; } = true;

    public bool ShowFriendsList { get; set; } = true;

    public bool NotifyOnLikes { get; set; } = true;
    public bool NotifyOnComments { get; set; } = true;
    public bool NotifyOnFriendRequests { get; set; } = true;
    public bool NotifyOnStoryTags { get; set; } = true;

    public DateTime? LastSeenAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsSuspended { get; set; } = false;
    public DateTime? SuspendedAt { get; set; }
    public string? SuspendReason { get; set; }
}
