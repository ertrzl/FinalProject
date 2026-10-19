using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Application.Common;

// The condition values the API accepts and returns (the enum names). The page shows them as
// Sıfır / Sıfır gibi / İyi / Orta; the validators and the browse filter check against this list.
public static class MarketplaceConditions
{
    public static readonly IReadOnlyList<string> All = Enum.GetNames<ListingCondition>();

    public static bool TryParse(string? value, out ListingCondition condition)
    {
        // Only the names count: a number like "7" is not accepted as a condition.
        condition = default;
        return value != null && All.Contains(value) && Enum.TryParse(value, out condition);
    }
}
