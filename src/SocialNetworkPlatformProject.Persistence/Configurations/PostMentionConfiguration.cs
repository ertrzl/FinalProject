using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class PostMentionConfiguration : IEntityTypeConfiguration<PostMention>
{
    public void Configure(EntityTypeBuilder<PostMention> builder)
    {
        builder.HasIndex(m => new { m.PostId, m.MentionedUserId }).IsUnique();

        builder.HasIndex(m => m.MentionedUserId);

        builder.HasOne(m => m.Post).WithMany().HasForeignKey(m => m.PostId).OnDelete(DeleteBehavior.Cascade);
    }
}
