using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class ListingBoostConfiguration : IEntityTypeConfiguration<ListingBoost>
{
    public void Configure(EntityTypeBuilder<ListingBoost> builder)
    {
        builder.HasIndex(b => new { b.SellerId, b.CreatedAt });
    }
}
