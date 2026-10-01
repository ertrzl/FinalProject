using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class GroupJoinRequestConfiguration : IEntityTypeConfiguration<GroupJoinRequest>
{
    public void Configure(EntityTypeBuilder<GroupJoinRequest> builder)
    {
        // One pending request per user per group — JoinAsync also checks this before inserting.
        builder.HasIndex(r => new { r.GroupId, r.UserId }).IsUnique();
    }
}
