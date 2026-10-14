using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// One thing an admin did (suspended someone, removed a post, closed a report...). Never edited or deleted by the app;
// the admin panel only reads it. The actor's name is copied in so the line stays readable after the account is gone.
public class AuditLog : BaseEntity
{
    public Guid ActorId { get; set; }
    public string ActorName { get; set; } = string.Empty;

    // A short code such as "user.suspend" or "post.remove" (the panel turns it into words).
    public string Action { get; set; } = string.Empty;

    public string? TargetType { get; set; }
    public Guid? TargetId { get; set; }

    // A readable detail: the name of the person, the start of the removed text, the reason given.
    public string? Details { get; set; }
}
