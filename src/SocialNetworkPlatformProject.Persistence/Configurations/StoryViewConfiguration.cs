using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class StoryViewConfiguration : IEntityTypeConfiguration<StoryView>
{
    public void Configure(EntityTypeBuilder<StoryView> builder)
    {
        builder.HasIndex(v => new { v.StoryId, v.ViewerId }).IsUnique();

        builder.HasOne(v => v.Story).WithMany().HasForeignKey(v => v.StoryId).OnDelete(DeleteBehavior.Cascade);
    }
}
