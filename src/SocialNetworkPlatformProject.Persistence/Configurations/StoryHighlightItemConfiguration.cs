using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class StoryHighlightItemConfiguration : IEntityTypeConfiguration<StoryHighlightItem>
{
    public void Configure(EntityTypeBuilder<StoryHighlightItem> builder)
    {
        builder.HasIndex(i => new { i.HighlightId, i.SourceStoryId }).IsUnique();

        builder.Property(i => i.MediaUrl).HasMaxLength(500);
        builder.HasIndex(i => i.MediaUrl);

        builder.Property(i => i.MusicTitle).HasMaxLength(150);
        builder.Property(i => i.MusicArtist).HasMaxLength(150);
        builder.Property(i => i.MusicUrl).HasMaxLength(500);
        builder.Property(i => i.MusicArtworkUrl).HasMaxLength(500);
    }
}
