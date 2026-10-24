using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class EventPhotoConfiguration : IEntityTypeConfiguration<EventPhoto>
{
    public void Configure(EntityTypeBuilder<EventPhoto> builder)
    {
        builder.Property(p => p.Url).IsRequired().HasMaxLength(500);

        builder.HasOne(p => p.Event)
            .WithMany(e => e.Photos)
            .HasForeignKey(p => p.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.EventId, p.CreatedAt });
        builder.HasIndex(p => new { p.EventId, p.UploadedByUserId });

        builder.HasIndex(p => p.Url);
    }
}
