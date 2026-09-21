using System.Linq.Expressions;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Marketplace;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;

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
            Location = dto.Location?.Trim(),
            Description = dto.Description?.Trim(),
            ImageUrl = imageUrl
        };

        await _listings.AddAsync(listing);
        await _listings.SaveChangesAsync();

        return (await BuildDtosAsync(new[] { listing }))[0];
    }

    public async Task<GetMarketplaceListingDto> GetByIdAsync(Guid listingId)
    {
        var listing = await _listings.GetByIdAsync(listingId)
            ?? throw new NotFoundException("Listing not found.");

        return (await BuildDtosAsync(new[] { listing }))[0];
    }

    public async Task<PagedResult<GetMarketplaceListingDto>> GetListingsAsync(string? category, string? search, int page, int pageSize)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var categoryFilter = string.IsNullOrWhiteSpace(category) ? null : category.Trim();
        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        Expression<Func<MarketplaceListing, bool>> filter = l =>
            (categoryFilter == null || l.Category == categoryFilter) &&
            (term == null || l.Title.Contains(term) || (l.Description != null && l.Description.Contains(term)));

        var total = await _listings.GetAll(filter).CountAsync();

        var listings = await _listings.GetAll(
                filter: filter,
                orderBy: l => l.CreatedAt,
                isDescending: true,
                asNoTracking: true,
                page: page,
                take: pageSize)
            .ToListAsync();

        return new PagedResult<GetMarketplaceListingDto>
        {
            Items = await BuildDtosAsync(listings),
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
        var listing = await _listings.GetByIdAsync(listingId)
            ?? throw new NotFoundException("Listing not found.");

        if (listing.SellerId != currentUserId)
            throw new ForbiddenException("You can only delete your own listings.");

        _listings.Delete(listing);
        await _listings.SaveChangesAsync();

        _files.Delete(listing.ImageUrl);
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
