using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class FriendRequestConfiguration : IEntityTypeConfiguration<FriendRequest>
{
    public void Configure(EntityTypeBuilder<FriendRequest> builder)
    {
        // At most one open request per direction: a double click or two tabs cannot create a second one.
        // Old (answered) requests are kept for history, so the rule only covers Pending.
        builder.HasIndex(r => new { r.SenderId, r.ReceiverId })
            .IsUnique()
            .HasFilter($"[Status] = {(int)FriendRequestStatus.Pending}");
    }
}
