using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class SavedListingConfiguration : IEntityTypeConfiguration<SavedListing>
{
    public void Configure(EntityTypeBuilder<SavedListing> builder)
    {
        builder.HasIndex(s => new { s.UserId, s.ListingId }).IsUnique();

        // Deleting a listing silently drops everyone's saved entry for it.
        builder.HasOne(s => s.Listing)
            .WithMany()
            .HasForeignKey(s => s.ListingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
