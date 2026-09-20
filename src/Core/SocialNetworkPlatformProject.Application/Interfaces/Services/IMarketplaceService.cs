using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Marketplace;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IMarketplaceService
{
    Task<GetMarketplaceListingDto> CreateAsync(Guid currentUserId, PostMarketplaceListingDto dto);

    Task<GetMarketplaceListingDto> GetByIdAsync(Guid listingId);

    Task<PagedResult<GetMarketplaceListingDto>> GetListingsAsync(string? category, string? search, int page, int pageSize);

    Task<List<GetMarketplaceListingDto>> GetMyListingsAsync(Guid currentUserId);

    Task DeleteAsync(Guid currentUserId, Guid listingId);
}
