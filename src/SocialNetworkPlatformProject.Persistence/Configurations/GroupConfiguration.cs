using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.HasMany(g => g.Members)
            .WithOne(m => m.Group)
            .HasForeignKey(m => m.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(g => g.JoinRequests)
            .WithOne(r => r.Group)
            .HasForeignKey(r => r.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(g => g.Invites)
            .WithOne(i => i.Group)
            .HasForeignKey(i => i.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
