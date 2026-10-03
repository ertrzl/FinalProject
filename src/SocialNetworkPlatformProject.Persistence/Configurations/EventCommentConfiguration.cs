using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class EventCommentConfiguration : IEntityTypeConfiguration<EventComment>
{
    public const int MaxContentLength = 1000;

    public void Configure(EntityTypeBuilder<EventComment> builder)
    {
        builder.Property(c => c.Content).IsRequired().HasMaxLength(MaxContentLength);
        builder.HasIndex(c => new { c.EventId, c.CreatedAt }); // the discussion list
    }
}
