using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.Property(m => m.Text).HasMaxLength(TextLimits.Message);

        builder.HasIndex(m => new { m.ConversationId, m.CreatedAt });

        builder.Property(m => m.MediaUrl).HasMaxLength(500);
        builder.HasIndex(m => m.MediaUrl).HasFilter("[MediaUrl] IS NOT NULL");
    }
}
