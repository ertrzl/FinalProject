using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Marketplace;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// Star ratings of marketplace sellers. Only the buyer of an accepted deal can rate, once per deal (editable).
public interface ISellerRatingService
{
    // Creates the rating for this deal, or replaces the buyer's earlier one (stars + comment).
    Task<GetSellerRatingDto> RateAsync(Guid currentUserId, Guid offerId, PostSellerRatingDto dto);

    Task DeleteAsync(Guid currentUserId, Guid offerId);

    Task<GetSellerRatingSummaryDto> GetSummaryAsync(Guid sellerId);

    // The seller's reviews, newest first.
    Task<PagedResult<GetSellerRatingDto>> GetSellerRatingsAsync(Guid sellerId, int page, int pageSize);
}
