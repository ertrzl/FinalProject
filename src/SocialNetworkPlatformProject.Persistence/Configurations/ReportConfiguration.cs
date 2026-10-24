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

        builder.HasIndex(r => new { r.Status, r.CreatedAt });

        builder.HasIndex(r => new { r.ReporterId, r.TargetType, r.TargetId }).IsUnique();
        builder.HasIndex(r => new { r.TargetType, r.TargetId });
    }
}
