using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class StoryHighlightConfiguration : IEntityTypeConfiguration<StoryHighlight>
{
    public void Configure(EntityTypeBuilder<StoryHighlight> builder)
    {
        builder.Property(h => h.Title).HasMaxLength(30).IsRequired();

        builder.HasIndex(h => new { h.UserId, h.CreatedAt });

        builder.HasMany(h => h.Items).WithOne(i => i.Highlight).HasForeignKey(i => i.HighlightId).OnDelete(DeleteBehavior.Cascade);
    }
}
