using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class EventPhotoConfiguration : IEntityTypeConfiguration<EventPhoto>
{
    public void Configure(EntityTypeBuilder<EventPhoto> builder)
    {
        builder.Property(p => p.Url).IsRequired().HasMaxLength(500);

        // Deleting an event takes its album with it (the files are removed by the code that deletes the event).
        builder.HasOne(p => p.Event)
            .WithMany(e => e.Photos)
            .HasForeignKey(p => p.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // The album of an event, newest first, and "how many did this person add?" for the per-person limit.
        builder.HasIndex(p => new { p.EventId, p.CreatedAt });
        builder.HasIndex(p => new { p.EventId, p.UploadedByUserId });

        // Who may open a file is decided from the row that holds its address (UploadsController).
        builder.HasIndex(p => p.Url);
    }
}
