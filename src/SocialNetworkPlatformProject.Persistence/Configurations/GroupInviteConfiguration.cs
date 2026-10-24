using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class GroupInviteConfiguration : IEntityTypeConfiguration<GroupInvite>
{
    public void Configure(EntityTypeBuilder<GroupInvite> builder)
    {
        builder.HasIndex(i => new { i.GroupId, i.UserId }).IsUnique();
    }
}
