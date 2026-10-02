using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialNetworkPlatformProject.Application.DTOs.Marketplace;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Extensions;

namespace SocialNetworkPlatformProject.Controllers;

// marketplace.html "Teklif Ver" button and the "Tekliflerim" tab (counter-offer bargaining)
[ApiController]
[Route("api/marketplace")]
[Authorize]
public class MarketplaceOffersController : ControllerBase
{
    private readonly IOfferService _offers;

    public MarketplaceOffersController(IOfferService offers)
    {
        _offers = offers;
    }

    [HttpPost("{listingId:guid}/offers")]
    public async Task<ActionResult<GetListingOfferDto>> CreateOffer(Guid listingId, PostOfferPriceDto dto)
    {
        return await _offers.CreateAsync(User.GetUserId(), listingId, dto);
    }

    [HttpGet("offers/received")]
    public async Task<ActionResult<List<GetListingOfferDto>>> GetReceived()
    {
        return await _offers.GetReceivedAsync(User.GetUserId());
    }

    [HttpGet("offers/sent")]
    public async Task<ActionResult<List<GetListingOfferDto>>> GetSent()
    {
        return await _offers.GetSentAsync(User.GetUserId());
    }

    [HttpPost("offers/{offerId:guid}/counter")]
    public async Task<ActionResult<GetListingOfferDto>> Counter(Guid offerId, PostOfferPriceDto dto)
    {
        return await _offers.CounterAsync(User.GetUserId(), offerId, dto);
    }

    [HttpPost("offers/{offerId:guid}/accept")]
    public async Task<ActionResult<GetListingOfferDto>> Accept(Guid offerId)
    {
        return await _offers.AcceptAsync(User.GetUserId(), offerId);
    }

    [HttpPost("offers/{offerId:guid}/reject")]
    public async Task<ActionResult<GetListingOfferDto>> Reject(Guid offerId)
    {
        return await _offers.RejectAsync(User.GetUserId(), offerId);
    }

    [HttpPost("offers/{offerId:guid}/withdraw")]
    public async Task<ActionResult<GetListingOfferDto>> Withdraw(Guid offerId)
    {
        return await _offers.WithdrawAsync(User.GetUserId(), offerId);
    }
}
