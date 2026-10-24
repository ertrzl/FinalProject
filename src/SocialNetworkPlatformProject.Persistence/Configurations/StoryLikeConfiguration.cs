using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class StoryLikeConfiguration : IEntityTypeConfiguration<StoryLike>
{
    public void Configure(EntityTypeBuilder<StoryLike> builder)
    {
        builder.HasIndex(l => new { l.StoryId, l.UserId }).IsUnique();

        builder.HasOne(l => l.Story).WithMany().HasForeignKey(l => l.StoryId).OnDelete(DeleteBehavior.Cascade);
    }
}
