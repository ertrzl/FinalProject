namespace SocialNetworkPlatformProject.Application.Common;

// Single source of truth for listing categories: the validators check against it and
// GET /api/marketplace/categories feeds the page's filter list and sell-form select.
public static class MarketplaceCategories
{
    public static readonly IReadOnlyList<string> All =
        new[] { "Elektronik", "Ev Eşyası", "Giyim", "Kitap", "Spor", "Araç" };
}
