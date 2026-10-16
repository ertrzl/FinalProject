using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.Property(m => m.Text).HasMaxLength(TextLimits.Message);

        // A conversation's history, and the newest message of each conversation in the list.
        builder.HasIndex(m => new { m.ConversationId, m.CreatedAt });

        // Who may open a photo is decided from the message that holds its address (UploadsController).
        builder.Property(m => m.MediaUrl).HasMaxLength(500);
        builder.HasIndex(m => m.MediaUrl).HasFilter("[MediaUrl] IS NOT NULL");
    }
}
