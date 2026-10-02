using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Marketplace;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class MarketplaceService : IMarketplaceService
{
    private readonly IMarketplaceListingRepository _listings;
    private readonly IUserRepository _users;
    private readonly IFileStorageService _files;
    private readonly IMapper _mapper;

    public MarketplaceService(
        IMarketplaceListingRepository listings,
        IUserRepository users,
        IFileStorageService files,
        IMapper mapper)
    {
        _listings = listings;
        _users = users;
        _files = files;
        _mapper = mapper;
    }

    public async Task<GetMarketplaceListingDto> CreateAsync(Guid currentUserId, PostMarketplaceListingDto dto)
    {
        string? imageUrl = null;
        if (dto.Image != null)
            imageUrl = await _files.SaveImageAsync(dto.Image, "marketplace");

        var listing = new MarketplaceListing
        {
            SellerId = currentUserId,
            Title = dto.Title.Trim(),
            Price = dto.Price,
            Category = dto.Category.Trim(),
            Location = NullIfBlank(dto.Location),
            Description = NullIfBlank(dto.Description),
            ImageUrl = imageUrl
        };

        await _listings.AddAsync(listing);
        await _listings.SaveChangesAsync();

        return (await BuildDtosAsync(new[] { listing }))[0];
    }

    public async Task<GetMarketplaceListingDto> UpdateAsync(Guid currentUserId, Guid listingId, PutMarketplaceListingDto dto)
    {
        var listing = await GetOwnedListingAsync(currentUserId, listingId, "You can only edit your own listings.");

        var oldImageUrl = listing.ImageUrl;
        var replacesImage = false;

        // The new file is saved first so a rejected upload leaves the old photo untouched.
        if (dto.Image != null)
        {
            listing.ImageUrl = await _files.SaveImageAsync(dto.Image, "marketplace");
            replacesImage = true;
        }
        else if (dto.RemoveImage)
        {
            listing.ImageUrl = null;
            replacesImage = true;
        }

        listing.Title = dto.Title.Trim();
        listing.Price = dto.Price;
        listing.Category = dto.Category.Trim();
        listing.Location = NullIfBlank(dto.Location);
        listing.Description = NullIfBlank(dto.Description);

        await _listings.SaveChangesAsync();

        if (replacesImage)
            _files.Delete(oldImageUrl);

        return (await BuildDtosAsync(new[] { listing }))[0];
    }

    public async Task<GetMarketplaceListingDto> SetStatusAsync(Guid currentUserId, Guid listingId, PutListingStatusDto dto)
    {
        if (!Enum.TryParse<ListingStatus>(dto.Status, out var status))
            throw new BadRequestException("Status must be either 'Active' or 'Sold'.");

        var listing = await GetOwnedListingAsync(currentUserId, listingId, "You can only change the status of your own listings.");

        listing.Status = status;
        await _listings.SaveChangesAsync();

        return (await BuildDtosAsync(new[] { listing }))[0];
    }

    public async Task<GetMarketplaceListingDto> GetByIdAsync(Guid listingId)
    {
        var listing = await _listings.GetByIdAsync(listingId)
            ?? throw new NotFoundException("Listing not found.");

        return (await BuildDtosAsync(new[] { listing }))[0];
    }

    public async Task<PagedResult<GetMarketplaceListingDto>> GetListingsAsync(MarketplaceListingQuery query)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        var category = NullIfBlank(query.Category);
        var term = NullIfBlank(query.Search);
        var location = NullIfBlank(query.Location);

        var listings = _listings.GetAll(l => l.Status == ListingStatus.Active, asNoTracking: true);

        if (category != null)
            listings = listings.Where(l => l.Category == category);

        if (term != null)
            listings = listings.Where(l => l.Title.Contains(term) || (l.Description != null && l.Description.Contains(term)));

        if (location != null)
            listings = listings.Where(l => l.Location != null && l.Location.Contains(location));

        if (query.MinPrice.HasValue)
            listings = listings.Where(l => l.Price >= query.MinPrice.Value);

        if (query.MaxPrice.HasValue)
            listings = listings.Where(l => l.Price <= query.MaxPrice.Value);

        var total = await listings.CountAsync();

        // Id is the final tie-breaker so paging stays stable when prices or timestamps collide.
        var ordered = query.Sort switch
        {
            MarketplaceSort.PriceAsc => listings.OrderBy(l => l.Price).ThenByDescending(l => l.CreatedAt).ThenBy(l => l.Id),
            MarketplaceSort.PriceDesc => listings.OrderByDescending(l => l.Price).ThenByDescending(l => l.CreatedAt).ThenBy(l => l.Id),
            _ => listings.OrderByDescending(l => l.CreatedAt).ThenBy(l => l.Id)
        };

        var items = await ordered.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<GetMarketplaceListingDto>
        {
            Items = await BuildDtosAsync(items),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<List<GetMarketplaceListingDto>> GetMyListingsAsync(Guid currentUserId)
    {
        var listings = await _listings.GetAll(
                filter: l => l.SellerId == currentUserId,
                orderBy: l => l.CreatedAt,
                isDescending: true,
                asNoTracking: true)
            .ToListAsync();

        return await BuildDtosAsync(listings);
    }

    public async Task DeleteAsync(Guid currentUserId, Guid listingId)
    {
        var listing = await GetOwnedListingAsync(currentUserId, listingId, "You can only delete your own listings.");

        _listings.Delete(listing);
        await _listings.SaveChangesAsync();

        _files.Delete(listing.ImageUrl);
    }

    private async Task<MarketplaceListing> GetOwnedListingAsync(Guid currentUserId, Guid listingId, string forbiddenMessage)
    {
        var listing = await _listings.GetByIdAsync(listingId)
            ?? throw new NotFoundException("Listing not found.");

        if (listing.SellerId != currentUserId)
            throw new ForbiddenException(forbiddenMessage);

        return listing;
    }

    private static string? NullIfBlank(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private async Task<List<GetMarketplaceListingDto>> BuildDtosAsync(IEnumerable<MarketplaceListing> listings)
    {
        var list = listings.ToList();
        var sellers = await _users.GetSummariesAsync(list.Select(l => l.SellerId));

        var dtos = _mapper.Map<List<GetMarketplaceListingDto>>(list);
        for (var i = 0; i < dtos.Count; i++)
        {
            if (sellers.TryGetValue(list[i].SellerId, out var seller))
            {
                dtos[i].SellerName = seller.FullName;
                dtos[i].SellerAvatarUrl = seller.AvatarUrl;
            }
        }

        return dtos;
    }
}
