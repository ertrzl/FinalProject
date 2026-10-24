using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class PostHashtagConfiguration : IEntityTypeConfiguration<PostHashtag>
{
    public void Configure(EntityTypeBuilder<PostHashtag> builder)
    {
        builder.HasIndex(ph => new { ph.PostId, ph.HashtagId }).IsUnique();

        builder.HasOne(ph => ph.Hashtag)
            .WithMany(h => h.Posts)
            .HasForeignKey(ph => ph.HashtagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
