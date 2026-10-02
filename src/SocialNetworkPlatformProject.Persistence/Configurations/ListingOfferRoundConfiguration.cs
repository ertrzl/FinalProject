using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class ListingOfferRoundConfiguration : IEntityTypeConfiguration<ListingOfferRound>
{
    public void Configure(EntityTypeBuilder<ListingOfferRound> builder)
    {
        builder.Property(r => r.Price).HasColumnType("decimal(18,2)");

        builder.HasOne(r => r.Offer)
            .WithMany(o => o.Rounds)
            .HasForeignKey(r => r.OfferId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(r => r.OfferId);
    }
}
