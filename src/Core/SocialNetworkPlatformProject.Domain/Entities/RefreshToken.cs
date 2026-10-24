using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public Guid SessionId { get; set; }

    public DateTime SessionStartedAt { get; set; } = DateTime.UtcNow;

    public string? DeviceName { get; set; }
    public string? IpAddress { get; set; }
}
