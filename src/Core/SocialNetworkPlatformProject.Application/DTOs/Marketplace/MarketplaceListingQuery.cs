namespace SocialNetworkPlatformProject.Application.DTOs.Marketplace;

// Query-string filters of GET /api/marketplace (sidebar on marketplace.html).
public class MarketplaceListingQuery
{
    public string? Category { get; set; }
    public string? Search { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Location { get; set; }
    public string? Condition { get; set; } // one of MarketplaceConditions.All; empty = any
    public bool IncludeSold { get; set; } // "Satılanları da göster": sold listings are left out unless asked for
    public Guid? SellerId { get; set; } // "Bu satıcının diğer ilanları"
    public string Sort { get; set; } = MarketplaceSort.Newest;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public static class MarketplaceSort
{
    public const string Newest = "newest";
    public const string PriceAsc = "price_asc";
    public const string PriceDesc = "price_desc";
    public const string Nearby = "nearby"; // "Bana yakın": the viewer's profile city first, then newest

    public static readonly IReadOnlyList<string> All = new[] { Newest, PriceAsc, PriceDesc, Nearby };
}
