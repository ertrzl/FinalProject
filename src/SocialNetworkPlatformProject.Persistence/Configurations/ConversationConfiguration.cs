using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.Property(c => c.DirectKey).HasMaxLength(80);
        builder.HasIndex(c => c.DirectKey).IsUnique().HasFilter("[DirectKey] IS NOT NULL");

        builder.HasIndex(c => c.GroupId).IsUnique().HasFilter("[GroupId] IS NOT NULL");

        builder.HasOne(c => c.Group)
            .WithMany()
            .HasForeignKey(c => c.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(c => c.Title).HasMaxLength(60);
        builder.Property(c => c.AvatarUrl).HasMaxLength(500);
        builder.HasIndex(c => c.AvatarUrl);

        builder.HasMany(c => c.Participants)
            .WithOne(p => p.Conversation)
            .HasForeignKey(p => p.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
