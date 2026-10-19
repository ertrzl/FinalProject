using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class MarketplaceListingConfiguration : IEntityTypeConfiguration<MarketplaceListing>
{
    public void Configure(EntityTypeBuilder<MarketplaceListing> builder)
    {
        builder.Property(m => m.Price)
            .HasColumnType("decimal(18,2)");

        builder.Property(m => m.RowVersion).IsRowVersion();

        builder.Property(m => m.Title).HasMaxLength(TextLimits.ListingTitle);
        builder.Property(m => m.Category).HasMaxLength(TextLimits.ListingCategory);
        builder.Property(m => m.Location).HasMaxLength(TextLimits.ListingLocation);
        builder.Property(m => m.Description).HasMaxLength(TextLimits.ListingDescription);

        // The public grid always filters on Status and sorts newest-first; "İlanlarım" filters on SellerId.
        builder.HasIndex(m => new { m.Status, m.CreatedAt });
        builder.HasIndex(m => m.SellerId);

        // "Similar listings" looks inside one category and leaves out the expired ones.
        builder.HasIndex(m => new { m.Category, m.Status, m.ExpiresAt });


        // The service always sets ExpiresAt itself; this only gives listings that existed before the field a month from
        // the migration (the migration may refine it per row, e.g. created + 30 days but at least a week from now).
        builder.Property(m => m.ExpiresAt).HasDefaultValueSql("DATEADD(day, 30, GETUTCDATE())");
    }
}
