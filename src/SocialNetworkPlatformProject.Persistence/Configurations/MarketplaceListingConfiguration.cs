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

        builder.HasIndex(m => new { m.Status, m.CreatedAt });
        builder.HasIndex(m => m.SellerId);

        builder.HasIndex(m => new { m.Category, m.Status, m.ExpiresAt });


        builder.Property(m => m.ExpiresAt).HasDefaultValueSql("DATEADD(day, 30, GETUTCDATE())");
    }
}
