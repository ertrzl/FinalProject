using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class ListingOfferConfiguration : IEntityTypeConfiguration<ListingOffer>
{
    public void Configure(EntityTypeBuilder<ListingOffer> builder)
    {
        builder.Property(o => o.CurrentPrice).HasColumnType("decimal(18,2)");
        builder.Property(o => o.RowVersion).IsRowVersion();

        builder.HasOne(o => o.Listing)
            .WithMany()
            .HasForeignKey(o => o.ListingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => new { o.ListingId, o.BuyerId })
            .IsUnique()
            .HasFilter("[Status] = 0");

        builder.HasIndex(o => new { o.SellerId, o.Status });
        builder.HasIndex(o => new { o.BuyerId, o.Status });
    }
}
