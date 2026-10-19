using SocialNetworkPlatformProject.Application.DTOs.Marketplace;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IMarketplaceService
{
    Task<GetMarketplaceListingDto> CreateAsync(Guid currentUserId, PostMarketplaceListingDto dto);

    // Seller-only: edits the listing's details/photo.
    Task<GetMarketplaceListingDto> UpdateAsync(Guid currentUserId, Guid listingId, PutMarketplaceListingDto dto);

    // Seller-only: Active / Reserved (promised to someone, no new offers) / Sold (hidden from the public grid).
    Task<GetMarketplaceListingDto> SetStatusAsync(Guid currentUserId, Guid listingId, PutListingStatusDto dto);

    // Seller-only "Yenile": another 30 days, for an expired listing or one in its last week; at most once per 24 hours.
    Task<GetMarketplaceListingDto> RenewAsync(Guid currentUserId, Guid listingId);

    // Seller-only "Öne Çıkar": the listing comes first in the grid for 24 hours; one at a time, once per 7 days per seller.
    Task<GetMarketplaceListingDto> FeatureAsync(Guid currentUserId, Guid listingId);

    // Also tells whether the viewer may message the seller (CanMessageSeller).
    Task<GetMarketplaceListingDto> GetByIdAsync(Guid currentUserId, Guid listingId);

    // Up to 6 listings on sale in the same category, the closest in price (not the seller's, not the viewer's, nobody blocked).
    Task<List<GetMarketplaceListingDto>> GetSimilarAsync(Guid currentUserId, Guid listingId);

    // Public grid: listings that are for sale (Active / Reserved, not expired; Sold ones only when asked), filtered/sorted per the query.
    Task<PagedResult<GetMarketplaceListingDto>> GetListingsAsync(Guid currentUserId, MarketplaceListingQuery query);

    // "İlanlarım": every listing of the current user, sold ones included.
    Task<List<GetMarketplaceListingDto>> GetMyListingsAsync(Guid currentUserId);

    // "Kaydedilenler": the user's favourites, newest saved first (sold ones stay, shown as sold).
    Task<List<GetMarketplaceListingDto>> GetSavedListingsAsync(Guid currentUserId);

    // Adds/removes the listing from the user's favourites; returns true when it is now saved.
    Task<bool> ToggleSaveAsync(Guid currentUserId, Guid listingId);

    // asAdmin: the site's admin removing it (only the admin panel passes true).
    Task DeleteAsync(Guid currentUserId, Guid listingId, bool asAdmin = false);
}
