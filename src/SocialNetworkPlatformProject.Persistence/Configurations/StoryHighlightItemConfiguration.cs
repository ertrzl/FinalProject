using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class StoryHighlightItemConfiguration : IEntityTypeConfiguration<StoryHighlightItem>
{
    public void Configure(EntityTypeBuilder<StoryHighlightItem> builder)
    {
        // A story is kept in a highlight once, even when two taps arrive at the same moment.
        builder.HasIndex(i => new { i.HighlightId, i.SourceStoryId }).IsUnique();

        // Who may open the file is decided from the highlight that holds its address (UploadsController).
        builder.Property(i => i.MediaUrl).HasMaxLength(500);
        builder.HasIndex(i => i.MediaUrl);

        builder.Property(i => i.MusicTitle).HasMaxLength(150);
        builder.Property(i => i.MusicArtist).HasMaxLength(150);
        builder.Property(i => i.MusicUrl).HasMaxLength(500);
        builder.Property(i => i.MusicArtworkUrl).HasMaxLength(500);
    }
}
