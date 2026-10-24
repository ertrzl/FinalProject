using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SocialNetworkPlatformProject.Domain.Entities;

namespace SocialNetworkPlatformProject.Persistence.Configurations;

public class GroupRuleConfiguration : IEntityTypeConfiguration<GroupRule>
{
    public void Configure(EntityTypeBuilder<GroupRule> builder)
    {
        builder.Property(r => r.Title).HasMaxLength(GroupLimits.RuleTitle);
        builder.Property(r => r.Text).HasMaxLength(GroupLimits.RuleText);

        builder.HasIndex(r => new { r.GroupId, r.SortOrder });
    }
}
