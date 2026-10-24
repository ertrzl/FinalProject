using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class AuditLog : BaseEntity
{
    public Guid ActorId { get; set; }
    public string ActorName { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string? TargetType { get; set; }
    public Guid? TargetId { get; set; }

    public string? Details { get; set; }
}
