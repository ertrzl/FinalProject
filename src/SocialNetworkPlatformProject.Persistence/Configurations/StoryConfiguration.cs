using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class StoryConfiguration : IEntityTypeConfiguration<Story>
{
    public void Configure(EntityTypeBuilder<Story> builder)
    {
        // The story bar: a user's stories that have not expired yet.
        builder.HasIndex(s => new { s.UserId, s.ExpiresAt });

        // Who may open a story's file is decided from the story that holds its address (UploadsController).
        builder.Property(s => s.MediaUrl).HasMaxLength(500);
        builder.HasIndex(s => s.MediaUrl);
    }
}
