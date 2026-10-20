using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class GroupPinnedPostConfiguration : IEntityTypeConfiguration<GroupPinnedPost>
{
    public void Configure(EntityTypeBuilder<GroupPinnedPost> builder)
    {
        // A post is pinned once; GroupPostService also checks before inserting.
        builder.HasIndex(p => new { p.GroupId, p.PostId }).IsUnique();

        // Deleting the post (or its group, which deletes the post) unpins it.
        builder.HasOne(p => p.Post)
            .WithMany()
            .HasForeignKey(p => p.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
