using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.Property(a => a.ActorName).HasMaxLength(200);
        builder.Property(a => a.Action).HasMaxLength(64);
        builder.Property(a => a.TargetType).HasMaxLength(32);
        builder.Property(a => a.Details).HasMaxLength(500);

        builder.HasIndex(a => a.CreatedAt);
        builder.HasIndex(a => a.Action);
    }
}
