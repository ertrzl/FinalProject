using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class StoryViewConfiguration : IEntityTypeConfiguration<StoryView>
{
    public void Configure(EntityTypeBuilder<StoryView> builder)
    {
        // A person is counted once per story, even when two tabs report the same view at the same moment.
        builder.HasIndex(v => new { v.StoryId, v.ViewerId }).IsUnique();

        builder.HasOne(v => v.Story).WithMany().HasForeignKey(v => v.StoryId).OnDelete(DeleteBehavior.Cascade);
    }
}
