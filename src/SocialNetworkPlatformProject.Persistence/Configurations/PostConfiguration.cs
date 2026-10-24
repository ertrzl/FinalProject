using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.Property(p => p.Text).HasMaxLength(TextLimits.Post);
        builder.ToTable(t => t.HasCheckConstraint("CK_Posts_Text_Length", $"LEN([Text]) <= {TextLimits.Post}"));

        builder.HasMany(p => p.Comments)
            .WithOne(c => c.Post)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Likes)
            .WithOne(l => l.Post)
            .HasForeignKey(l => l.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.SavedBy)
            .WithOne(s => s.Post)
            .HasForeignKey(s => s.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Hashtags)
            .WithOne(h => h.Post)
            .HasForeignKey(h => h.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Poll)
            .WithOne(poll => poll.Post)
            .HasForeignKey<Poll>(poll => poll.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.SharedPost)
            .WithMany()
            .HasForeignKey(p => p.SharedPostId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.SharedPostId);

        builder.HasIndex(p => new { p.AuthorId, p.CreatedAt });
        builder.HasIndex(p => new { p.GroupId, p.CreatedAt });

        builder.HasOne(p => p.Group)
            .WithMany()
            .HasForeignKey(p => p.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
