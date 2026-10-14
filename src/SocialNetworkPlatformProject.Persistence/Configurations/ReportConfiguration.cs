using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.Property(r => r.TargetSummary).HasMaxLength(300);
        builder.Property(r => r.Details).HasMaxLength(1000);
        builder.Property(r => r.AdminNote).HasMaxLength(500);

        // The admin's queue: pending first, newest first.
        builder.HasIndex(r => new { r.Status, r.CreatedAt });

        // One open report per person and thing: reporting the same post twice while it waits adds nothing.
        builder.HasIndex(r => new { r.ReporterId, r.TargetType, r.TargetId });
        builder.HasIndex(r => new { r.TargetType, r.TargetId });
    }
}
