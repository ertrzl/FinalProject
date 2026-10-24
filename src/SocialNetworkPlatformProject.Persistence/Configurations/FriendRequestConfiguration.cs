using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class FriendRequestConfiguration : IEntityTypeConfiguration<FriendRequest>
{
    public void Configure(EntityTypeBuilder<FriendRequest> builder)
    {
        builder.HasIndex(r => new { r.SenderId, r.ReceiverId })
            .IsUnique()
            .HasFilter($"[Status] = {(int)FriendRequestStatus.Pending}");

        builder.HasIndex(r => new { r.ReceiverId, r.Status });
    }
}
