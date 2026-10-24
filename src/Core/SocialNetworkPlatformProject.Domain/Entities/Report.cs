using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class Report : BaseEntity
{
    public Guid ReporterId { get; set; }

    public ReportTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }

    public Guid TargetOwnerId { get; set; }

    public string TargetSummary { get; set; } = string.Empty;

    public ReportReason Reason { get; set; }
    public string? Details { get; set; }

    public ReportStatus Status { get; set; } = ReportStatus.Pending;
    public Guid? ResolvedById { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public string? AdminNote { get; set; }
}
