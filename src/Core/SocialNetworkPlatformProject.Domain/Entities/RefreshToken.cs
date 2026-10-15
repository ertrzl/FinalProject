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

    // One sign-in on one device is a session. Each renewal makes a new token row, but all rows of a session share this id
    // (the access tokens carry it too), so "end this session" can cut off the refresh token and the access tokens together.
    public Guid SessionId { get; set; }

    // When the session began (copied from row to row on every renewal); this row's own CreatedAt is the last renewal.
    public DateTime SessionStartedAt { get; set; } = DateTime.UtcNow;

    // What the open-sessions list shows: "Chrome · Windows" and the address the request came from.
    public string? DeviceName { get; set; }
    public string? IpAddress { get; set; }
}
