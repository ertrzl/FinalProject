using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Marketplace;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IMarketplaceService
{
    Task<GetMarketplaceListingDto> CreateAsync(Guid currentUserId, PostMarketplaceListingDto dto);

    // Seller-only: edits the listing's details/photo.
    Task<GetMarketplaceListingDto> UpdateAsync(Guid currentUserId, Guid listingId, PutMarketplaceListingDto dto);

    // Seller-only: marks a listing Sold (hidden from the public grid) or puts it back on sale.
    Task<GetMarketplaceListingDto> SetStatusAsync(Guid currentUserId, Guid listingId, PutListingStatusDto dto);

    Task<GetMarketplaceListingDto> GetByIdAsync(Guid currentUserId, Guid listingId);

    // Public grid: active listings only, filtered/sorted per the query.
    Task<PagedResult<GetMarketplaceListingDto>> GetListingsAsync(Guid currentUserId, MarketplaceListingQuery query);

    // "İlanlarım": every listing of the current user, sold ones included.
    Task<List<GetMarketplaceListingDto>> GetMyListingsAsync(Guid currentUserId);

    // "Kaydedilenler": the user's favourites, newest saved first (sold ones stay, shown as sold).
    Task<List<GetMarketplaceListingDto>> GetSavedListingsAsync(Guid currentUserId);

    // Adds/removes the listing from the user's favourites; returns true when it is now saved.
    Task<bool> ToggleSaveAsync(Guid currentUserId, Guid listingId);

    Task DeleteAsync(Guid currentUserId, Guid listingId);
}
