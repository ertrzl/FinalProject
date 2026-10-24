using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class MessageReactionConfiguration : IEntityTypeConfiguration<MessageReaction>
{
    public void Configure(EntityTypeBuilder<MessageReaction> builder)
    {
        builder.Property(r => r.Emoji).HasMaxLength(16);

        builder.HasIndex(r => new { r.MessageId, r.UserId }).IsUnique();

        builder.HasOne(r => r.Message).WithMany().HasForeignKey(r => r.MessageId).OnDelete(DeleteBehavior.Cascade);
    }
}
