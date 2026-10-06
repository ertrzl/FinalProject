using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.Property(n => n.Amount).HasColumnType("decimal(18,2)");
        builder.Property(n => n.Subject).HasMaxLength(200); // event titles are capped at 150

        // "My notifications", newest first, and the unread badge: both start from the recipient.
        builder.HasIndex(n => new { n.RecipientId, n.CreatedAt });
    }
}
