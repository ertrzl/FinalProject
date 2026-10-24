using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.Property(g => g.PairKey).HasMaxLength(80);
        builder.Property(g => g.Board).HasMaxLength(9);
        builder.Property(g => g.Turn).HasConversion<string>().HasMaxLength(1);
        builder.Property(g => g.Rounds).HasMaxLength(12);
        builder.Property(g => g.InviterBoard).HasMaxLength(100);
        builder.Property(g => g.InviteeBoard).HasMaxLength(100);
        builder.Property(g => g.InviterShots).HasMaxLength(100);
        builder.Property(g => g.InviteeShots).HasMaxLength(100);
        builder.Property(g => g.RowVersion).IsRowVersion();

        builder.HasIndex(g => g.PairKey)
            .IsUnique()
            .HasFilter($"[Status] IN ({(int)GameStatus.Invited}, {(int)GameStatus.Active})");

        builder.HasIndex(g => g.InviterId);
        builder.HasIndex(g => g.InviteeId);
    }
}
