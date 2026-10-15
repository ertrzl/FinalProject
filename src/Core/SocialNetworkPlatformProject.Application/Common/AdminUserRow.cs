namespace SocialNetworkPlatformProject.Application.Common;

// What the admin panel's user list needs about one account (UserSummary leaves out the e-mail and the suspension).
public class AdminUserRow
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastSeenAt { get; init; }
    public bool IsAdmin { get; init; }
    public bool IsSuspended { get; init; }
    public DateTime? SuspendedAt { get; init; }
    public string? SuspendReason { get; init; }
}
