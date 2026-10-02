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

    Task<GetMarketplaceListingDto> GetByIdAsync(Guid listingId);

    // Public grid: active listings only, filtered/sorted per the query.
    Task<PagedResult<GetMarketplaceListingDto>> GetListingsAsync(MarketplaceListingQuery query);

    // "İlanlarım": every listing of the current user, sold ones included.
    Task<List<GetMarketplaceListingDto>> GetMyListingsAsync(Guid currentUserId);

    Task DeleteAsync(Guid currentUserId, Guid listingId);
}
