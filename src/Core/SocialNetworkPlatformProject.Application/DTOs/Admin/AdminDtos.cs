using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Application.DTOs.Admin;

public class PostReportDto
{
    public ReportTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public ReportReason Reason { get; set; }
    public string? Details { get; set; }
}

public class GetReportDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public ReportStatus Status { get; set; }
    public ReportReason Reason { get; set; }
    public string? Details { get; set; }

    public Guid ReporterId { get; set; }
    public string ReporterName { get; set; } = string.Empty;

    public ReportTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public string TargetSummary { get; set; } = string.Empty;

    public Guid TargetOwnerId { get; set; }
    public string TargetOwnerName { get; set; } = string.Empty;
    public bool TargetOwnerSuspended { get; set; }

    public bool TargetExists { get; set; }

    public int ReportsAboutTarget { get; set; }

    public string? ResolvedByName { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? AdminNote { get; set; }
}

public class ResolveReportDto
{
    public bool Dismiss { get; set; }

    public string? Note { get; set; }

    public bool RemoveContent { get; set; }

    public bool SuspendOwner { get; set; }
}

public class SuspendUserDto
{
    public string? Reason { get; set; }
}

public class GetAdminStatsDto
{
    public int Users { get; set; }
    public int SuspendedUsers { get; set; }
    public int NewUsersThisWeek { get; set; }
    public int Posts { get; set; }
    public int PostsToday { get; set; }
    public int Listings { get; set; }
    public int Events { get; set; }
    public int Groups { get; set; }
    public int PendingReports { get; set; }
    public int ResolvedReports { get; set; }
}

public class GetAdminUserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsSuspended { get; set; }
    public DateTime? SuspendedAt { get; set; }
    public string? SuspendReason { get; set; }
    public int PostCount { get; set; }
    public int ReportsAgainst { get; set; }
}

public class GetAdminContentDto
{
    public string Type { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class RemoveContentDto
{
    public string? Note { get; set; }
}

public class GetAuditLogDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid ActorId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? TargetType { get; set; }
    public Guid? TargetId { get; set; }
    public string? Details { get; set; }
}
