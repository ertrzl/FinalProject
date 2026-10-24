using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class PollVoteConfiguration : IEntityTypeConfiguration<PollVote>
{
    public void Configure(EntityTypeBuilder<PollVote> builder)
    {
        builder.HasIndex(v => new { v.PollId, v.UserId }).IsUnique();

        builder.HasOne(v => v.Option)
            .WithMany()
            .HasForeignKey(v => v.OptionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
