using SocialNetworkPlatformProject.Application.DTOs.Marketplace;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IMarketplaceService
{
    Task<GetMarketplaceListingDto> CreateAsync(Guid currentUserId, PostMarketplaceListingDto dto);

    Task<GetMarketplaceListingDto> UpdateAsync(Guid currentUserId, Guid listingId, PutMarketplaceListingDto dto);

    Task<GetMarketplaceListingDto> SetStatusAsync(Guid currentUserId, Guid listingId, PutListingStatusDto dto);

    Task<GetMarketplaceListingDto> RenewAsync(Guid currentUserId, Guid listingId);

    Task<GetMarketplaceListingDto> FeatureAsync(Guid currentUserId, Guid listingId);

    Task<GetMarketplaceListingDto> GetByIdAsync(Guid currentUserId, Guid listingId);

    Task<List<GetMarketplaceListingDto>> GetSimilarAsync(Guid currentUserId, Guid listingId);

    Task<PagedResult<GetMarketplaceListingDto>> GetListingsAsync(Guid currentUserId, MarketplaceListingQuery query);

    Task<List<GetMarketplaceListingDto>> GetMyListingsAsync(Guid currentUserId);

    Task<List<GetMarketplaceListingDto>> GetSavedListingsAsync(Guid currentUserId);

    Task<bool> ToggleSaveAsync(Guid currentUserId, Guid listingId);

    Task DeleteAsync(Guid currentUserId, Guid listingId, bool asAdmin = false);
}
