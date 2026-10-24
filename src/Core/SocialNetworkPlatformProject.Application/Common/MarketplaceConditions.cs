using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Application.Common;

public static class MarketplaceConditions
{
    public static readonly IReadOnlyList<string> All = Enum.GetNames<ListingCondition>();

    public static bool TryParse(string? value, out ListingCondition condition)
    {
        condition = default;
        return value != null && All.Contains(value) && Enum.TryParse(value, out condition);
    }
}
