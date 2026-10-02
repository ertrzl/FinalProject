using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Marketplace;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Extensions;

namespace SocialNetworkPlatformProject.Controllers;

// marketplace.html "Satıcıyı Puanla" modal, the seller reviews modal, and the rating badge on profile.html
[ApiController]
[Route("api/marketplace")]
[Authorize]
public class MarketplaceRatingsController : ControllerBase
{
    private readonly ISellerRatingService _ratings;

    public MarketplaceRatingsController(ISellerRatingService ratings)
    {
        _ratings = ratings;
    }

    [HttpPut("offers/{offerId:guid}/rating")]
    public async Task<ActionResult<GetSellerRatingDto>> Rate(Guid offerId, PostSellerRatingDto dto)
    {
        return await _ratings.RateAsync(User.GetUserId(), offerId, dto);
    }

    [HttpDelete("offers/{offerId:guid}/rating")]
    public async Task<IActionResult> DeleteRating(Guid offerId)
    {
        await _ratings.DeleteAsync(User.GetUserId(), offerId);
        return NoContent();
    }

    [HttpGet("sellers/{sellerId:guid}/rating-summary")]
    public async Task<ActionResult<GetSellerRatingSummaryDto>> GetSummary(Guid sellerId)
    {
        return await _ratings.GetSummaryAsync(sellerId);
    }

    [HttpGet("sellers/{sellerId:guid}/ratings")]
    public async Task<ActionResult<PagedResult<GetSellerRatingDto>>> GetRatings(
        Guid sellerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        return await _ratings.GetSellerRatingsAsync(sellerId, page, pageSize);
    }
}
