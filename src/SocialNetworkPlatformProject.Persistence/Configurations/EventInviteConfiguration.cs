using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class EventInviteConfiguration : IEntityTypeConfiguration<EventInvite>
{
    public void Configure(EntityTypeBuilder<EventInvite> builder)
    {
        builder.HasIndex(i => new { i.EventId, i.InvitedUserId }).IsUnique();
        builder.HasIndex(i => i.InvitedUserId);
    }
}
