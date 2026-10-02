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
    public async Task<ActionResult<PagedResult<GetMarketplaceListingDto>>> GetListings([FromQuery] MarketplaceListingQuery query)
    {
        return await _marketplace.GetListingsAsync(User.GetUserId(), query);
    }

    [HttpGet("categories")]
    public ActionResult<IReadOnlyList<string>> GetCategories()
    {
        return Ok(MarketplaceCategories.All);
    }

    [HttpGet("mine")]
    public async Task<ActionResult<List<GetMarketplaceListingDto>>> GetMyListings()
    {
        return await _marketplace.GetMyListingsAsync(User.GetUserId());
    }

    [HttpGet("saved")]
    public async Task<ActionResult<List<GetMarketplaceListingDto>>> GetSavedListings()
    {
        return await _marketplace.GetSavedListingsAsync(User.GetUserId());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetMarketplaceListingDto>> GetById(Guid id)
    {
        return await _marketplace.GetByIdAsync(User.GetUserId(), id);
    }

    [HttpPost]
    public async Task<ActionResult<GetMarketplaceListingDto>> Create([FromForm] PostMarketplaceListingDto dto)
    {
        var listing = await _marketplace.CreateAsync(User.GetUserId(), dto);
        return CreatedAtAction(nameof(GetById), new { id = listing.Id }, listing);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<GetMarketplaceListingDto>> Update(Guid id, [FromForm] PutMarketplaceListingDto dto)
    {
        return await _marketplace.UpdateAsync(User.GetUserId(), id, dto);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<GetMarketplaceListingDto>> SetStatus(Guid id, PutListingStatusDto dto)
    {
        return await _marketplace.SetStatusAsync(User.GetUserId(), id, dto);
    }

    [HttpPost("{id:guid}/save")]
    public async Task<ActionResult<bool>> ToggleSave(Guid id)
    {
        return await _marketplace.ToggleSaveAsync(User.GetUserId(), id);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _marketplace.DeleteAsync(User.GetUserId(), id);
        return NoContent();
    }
}
