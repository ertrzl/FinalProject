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

        // Deleting a listing removes its negotiations (and, through them, their rounds).
        builder.HasOne(o => o.Listing)
            .WithMany()
            .HasForeignKey(o => o.ListingId)
            .OnDelete(DeleteBehavior.Cascade);

        // A buyer can only have one running negotiation per listing (finished ones don't count: [Status] = 0 is Open).
        builder.HasIndex(o => new { o.ListingId, o.BuyerId })
            .IsUnique()
            .HasFilter("[Status] = 0");

        // The "Gelen / Verdiğim teklifler" inboxes.
        builder.HasIndex(o => new { o.SellerId, o.Status });
        builder.HasIndex(o => new { o.BuyerId, o.Status });
    }
}
