using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class SellerRatingConfiguration : IEntityTypeConfiguration<SellerRating>
{
    public void Configure(EntityTypeBuilder<SellerRating> builder)
    {
        builder.Property(r => r.ListingTitle).HasMaxLength(150);
        builder.Property(r => r.Comment).HasMaxLength(500);

        // One rating per deal. OfferId has no foreign key on purpose (see SellerRating).
        builder.HasIndex(r => r.OfferId).IsUnique();

        // The average/count of a seller and the reviews list both read by SellerId.
        builder.HasIndex(r => r.SellerId);

        builder.ToTable(t => t.HasCheckConstraint("CK_SellerRatings_Stars", "[Stars] BETWEEN 1 AND 5"));
    }
}
