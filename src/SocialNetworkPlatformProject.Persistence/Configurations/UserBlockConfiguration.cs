using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class UserBlockConfiguration : IEntityTypeConfiguration<UserBlock>
{
    public void Configure(EntityTypeBuilder<UserBlock> builder)
    {
        // One block per pair and direction; the index also answers "whom did this person block".
        builder.HasIndex(b => new { b.BlockerId, b.BlockedId }).IsUnique();

        // "Who blocked this person" is asked on every post list, so it gets its own index.
        builder.HasIndex(b => b.BlockedId);
    }
}
