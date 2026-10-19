using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.Property(g => g.PairKey).HasMaxLength(80); // two 36-character ids and a colon
        builder.Property(g => g.Board).HasMaxLength(9);
        builder.Property(g => g.Turn).HasConversion<string>().HasMaxLength(1); // stored as the letter itself
        builder.Property(g => g.Rounds).HasMaxLength(12); // three rounds, two digits each, with room to spare
        builder.Property(g => g.InviterBoard).HasMaxLength(100); // a 10 x 10 board
        builder.Property(g => g.InviteeBoard).HasMaxLength(100);
        builder.Property(g => g.InviterShots).HasMaxLength(100);
        builder.Property(g => g.InviteeShots).HasMaxLength(100);
        builder.Property(g => g.RowVersion).IsRowVersion();

        // One invitation or running game per pair of friends, even when two invitations arrive at the same moment.
        // Finished, declined, cancelled and expired games are history and don't count.
        builder.HasIndex(g => g.PairKey)
            .IsUnique()
            .HasFilter($"[Status] IN ({(int)GameStatus.Invited}, {(int)GameStatus.Active})");

        // "My games": the hub looks for the person on either side.
        builder.HasIndex(g => g.InviterId);
        builder.HasIndex(g => g.InviteeId);
    }
}
