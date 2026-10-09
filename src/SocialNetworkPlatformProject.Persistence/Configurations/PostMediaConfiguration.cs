using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class PostMediaConfiguration : IEntityTypeConfiguration<PostMedia>
{
    public void Configure(EntityTypeBuilder<PostMedia> builder)
    {
        builder.Property(m => m.Url).HasMaxLength(500);

        // A post's photos in the author's order.
        builder.HasIndex(m => new { m.PostId, m.SortOrder });

        builder.HasOne(m => m.Post)
            .WithMany(p => p.Media)
            .HasForeignKey(m => m.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
