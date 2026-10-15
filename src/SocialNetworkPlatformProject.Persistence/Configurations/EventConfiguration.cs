using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Persistence.Converters;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.Property(e => e.StartsAt).HasConversion<UtcDateTimeConverter>();
        builder.Property(e => e.EndsAt).HasConversion<UtcDateTimeConverter>();

        // The "Yaklaşan" list and every "has it finished?" check filter on EndsAt.
        builder.HasIndex(e => e.EndsAt);

        builder.Property(e => e.RowVersion).IsRowVersion();
        builder.Property(e => e.Title).HasMaxLength(TextLimits.EventTitle);
        builder.Property(e => e.Location).HasMaxLength(TextLimits.EventLocation);
        builder.Property(e => e.Description).HasMaxLength(TextLimits.EventDescription);
        builder.Property(e => e.OnlineLink).HasMaxLength(500);

        // Who may open an event's cover is decided from the event that holds its address (UploadsController).
        builder.Property(e => e.CoverImageUrl).HasMaxLength(500);
        builder.HasIndex(e => e.CoverImageUrl).HasFilter("[CoverImageUrl] IS NOT NULL");

        // Deleting a group takes its events (and their attendees, invites and comments) with it.
        builder.HasOne(e => e.Group)
            .WithMany()
            .HasForeignKey(e => e.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Invites)
            .WithOne(i => i.Event)
            .HasForeignKey(i => i.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Comments)
            .WithOne(c => c.Event)
            .HasForeignKey(c => c.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Attendees)
            .WithOne(a => a.Event)
            .HasForeignKey(a => a.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
