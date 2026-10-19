using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class PostMentionConfiguration : IEntityTypeConfiguration<PostMention>
{
    public void Configure(EntityTypeBuilder<PostMention> builder)
    {
        // A person is tagged in a post once.
        builder.HasIndex(m => new { m.PostId, m.MentionedUserId }).IsUnique();

        // "Posts I am tagged in" (profile page) and the clean-up when an account is deleted.
        builder.HasIndex(m => m.MentionedUserId);

        builder.HasOne(m => m.Post).WithMany().HasForeignKey(m => m.PostId).OnDelete(DeleteBehavior.Cascade);
    }
}
