using SocialNetworkPlatformProject.Application.DTOs.Marketplace;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface ISellerRatingService
{
    Task<GetSellerRatingDto> RateAsync(Guid currentUserId, Guid offerId, PostSellerRatingDto dto);

    Task DeleteAsync(Guid currentUserId, Guid offerId);

    Task<GetSellerRatingSummaryDto> GetSummaryAsync(Guid sellerId);

    Task<PagedResult<GetSellerRatingDto>> GetSellerRatingsAsync(Guid sellerId, int page, int pageSize);
}
