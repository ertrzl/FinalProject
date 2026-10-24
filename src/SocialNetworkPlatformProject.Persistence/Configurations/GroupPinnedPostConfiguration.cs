using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class GroupPinnedPostConfiguration : IEntityTypeConfiguration<GroupPinnedPost>
{
    public void Configure(EntityTypeBuilder<GroupPinnedPost> builder)
    {
        builder.HasIndex(p => new { p.GroupId, p.PostId }).IsUnique();

        builder.HasOne(p => p.Post)
            .WithMany()
            .HasForeignKey(p => p.PostId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
