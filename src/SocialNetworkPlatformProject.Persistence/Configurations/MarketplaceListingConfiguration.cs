using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class MarketplaceListingConfiguration : IEntityTypeConfiguration<MarketplaceListing>
{
    public void Configure(EntityTypeBuilder<MarketplaceListing> builder)
    {
        builder.Property(m => m.Price)
            .HasColumnType("decimal(18,2)");

        // The public grid always filters on Status and sorts newest-first; "İlanlarım" filters on SellerId.
        builder.HasIndex(m => new { m.Status, m.CreatedAt });
        builder.HasIndex(m => m.SellerId);
    }
}
