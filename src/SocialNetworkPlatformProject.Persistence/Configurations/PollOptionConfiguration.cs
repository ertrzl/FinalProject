using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class PollOptionConfiguration : IEntityTypeConfiguration<PollOption>
{
    public void Configure(EntityTypeBuilder<PollOption> builder)
    {
        builder.Property(o => o.Text).HasMaxLength(PollLimits.MaxOptionLength);

        // A poll's answers in the order the author wrote them.
        builder.HasIndex(o => new { o.PollId, o.SortOrder });
    }
}
