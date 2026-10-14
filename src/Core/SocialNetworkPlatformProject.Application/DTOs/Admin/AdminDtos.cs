using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Application.DTOs.Admin;

// What an ordinary person sends to report something (POST api/reports).
public class PostReportDto
{
    public ReportTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public ReportReason Reason { get; set; }
    public string? Details { get; set; }
}

// One report in the admin's list.
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

    // False once the post / comment / listing / account is gone (the summary above is what it was).
    public bool TargetExists { get; set; }

    // How many reports there are about the same thing (the pile-up is a sign worth seeing).
    public int ReportsAboutTarget { get; set; }

    public string? ResolvedByName { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? AdminNote { get; set; }
}

// An admin closing a report. The actions are optional and done together with it.
public class ResolveReportDto
{
    // The report is dismissed (nothing wrong) instead of resolved.
    public bool Dismiss { get; set; }

    public string? Note { get; set; }

    // Resolve only: remove the reported post / comment / listing.
    public bool RemoveContent { get; set; }

    // Resolve only: suspend the person behind it.
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

// One account in the admin's user list.
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

// A thing an admin can find and remove (post, comment, listing, event or group).
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
