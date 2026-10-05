using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// Lets the frontend get a fresh JWT without asking the user to log in again.
// One row per issued refresh token; each use rotates it (old one revoked, a new one issued).
// Only a hash of the token is stored, so a copy of the table is not a set of working logins.
public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }

    // Set when the token was used up (rotation), logged out, or revoked after a password change. Null = still usable.
    public DateTime? RevokedAt { get; set; }
}
