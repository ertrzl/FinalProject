using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Marketplace;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Extensions;

namespace SocialNetworkPlatformProject.Controllers;

// marketplace.html listing cards + "Ürün Sat" modal
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MarketplaceController : ControllerBase
{
    private readonly IMarketplaceService _marketplace;

    public MarketplaceController(IMarketplaceService marketplace)
    {
        _marketplace = marketplace;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<GetMarketplaceListingDto>>> GetListings(
        [FromQuery] string? category, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        return await _marketplace.GetListingsAsync(category, search, page, pageSize);
    }

    [HttpGet("mine")]
    public async Task<ActionResult<List<GetMarketplaceListingDto>>> GetMyListings()
    {
        return await _marketplace.GetMyListingsAsync(User.GetUserId());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetMarketplaceListingDto>> GetById(Guid id)
    {
        return await _marketplace.GetByIdAsync(id);
    }

    [HttpPost]
    public async Task<ActionResult<GetMarketplaceListingDto>> Create([FromForm] PostMarketplaceListingDto dto)
    {
        var listing = await _marketplace.CreateAsync(User.GetUserId(), dto);
        return CreatedAtAction(nameof(GetById), new { id = listing.Id }, listing);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _marketplace.DeleteAsync(User.GetUserId(), id);
        return NoContent();
    }
}
