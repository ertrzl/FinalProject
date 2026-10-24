using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class StoryTagConfiguration : IEntityTypeConfiguration<StoryTag>
{
    public void Configure(EntityTypeBuilder<StoryTag> builder)
    {
        builder.HasIndex(t => new { t.StoryId, t.TaggedUserId }).IsUnique();

        builder.HasIndex(t => t.TaggedUserId);

        builder.HasOne(t => t.Story).WithMany().HasForeignKey(t => t.StoryId).OnDelete(DeleteBehavior.Cascade);
    }
}
