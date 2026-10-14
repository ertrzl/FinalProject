using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

// A person's report of a post, comment, profile or listing. Kept as a plain row (no foreign keys), like UserBlock:
// the content may be deleted later (that is often the outcome), and the report must still say what it was about.
public class Report : BaseEntity
{
    public Guid ReporterId { get; set; }

    public ReportTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }

    // The person behind the reported thing (for a profile: the profile's owner), so the admin can act on them too.
    public Guid TargetOwnerId { get; set; }

    // The start of the reported text (or the name of the profile / listing) as it was when reported.
    public string TargetSummary { get; set; } = string.Empty;

    public ReportReason Reason { get; set; }
    public string? Details { get; set; }

    public ReportStatus Status { get; set; } = ReportStatus.Pending;
    public Guid? ResolvedById { get; set; }
    public DateTime? ResolvedAt { get; set; }

    // What the admin decided and did, in a few words (the audit log has the details of each action).
    public string? AdminNote { get; set; }
}
