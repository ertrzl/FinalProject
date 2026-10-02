using AutoMapper;
using Microsoft.AspNetCore.Http;
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
    // Every DTO needs the photos (cover + carousel), so listings are always loaded together with them.
    private static readonly string[] ListingIncludes = { "Images" };
    private static readonly string[] SavedListingIncludes = { "Listing.Images" };

    private readonly IMarketplaceListingRepository _listings;
    private readonly IListingImageRepository _images;
    private readonly ISavedListingRepository _saved;
    private readonly IListingOfferRepository _offers;
    private readonly ISellerRatingRepository _ratings;
    private readonly IOfferService _offerService;
    private readonly IUserRepository _users;
    private readonly IFileStorageService _files;
    private readonly IMapper _mapper;

    public MarketplaceService(
        IMarketplaceListingRepository listings,
        IListingImageRepository images,
        ISavedListingRepository saved,
        IListingOfferRepository offers,
        ISellerRatingRepository ratings,
        IOfferService offerService,
        IUserRepository users,
        IFileStorageService files,
        IMapper mapper)
    {
        _listings = listings;
        _images = images;
        _saved = saved;
        _offers = offers;
        _ratings = ratings;
        _offerService = offerService;
        _users = users;
        _files = files;
        _mapper = mapper;
    }

    public async Task<GetMarketplaceListingDto> CreateAsync(Guid currentUserId, PostMarketplaceListingDto dto)
    {
        var listing = new MarketplaceListing
        {
            SellerId = currentUserId,
            Title = dto.Title.Trim(),
            Price = dto.Price,
            Category = dto.Category.Trim(),
            Location = NullIfBlank(dto.Location),
            Description = NullIfBlank(dto.Description)
        };

        foreach (var image in await SaveImagesAsync(dto.Images))
            listing.Images.Add(image);

        await _listings.AddAsync(listing);
        await _listings.SaveChangesAsync();

        return (await BuildDtosAsync(new[] { listing }, currentUserId))[0];
    }

    public async Task<GetMarketplaceListingDto> UpdateAsync(Guid currentUserId, Guid listingId, PutMarketplaceListingDto dto)
    {
        var listing = await GetOwnedListingAsync(currentUserId, listingId, "You can only edit your own listings.");

        // Open negotiations were agreed against the current asking price, so it can't move underneath them.
        if (dto.Price != listing.Price && await _offerService.HasOpenOffersAsync(listing.Id))
            throw new BadRequestException("The price can't be changed while there are open offers on this listing.");

        var removeIds = dto.RemoveImageIds.Distinct().ToList();
        var removed = listing.Images.Where(i => removeIds.Contains(i.Id)).ToList();
        if (removed.Count != removeIds.Count)
            throw new BadRequestException("One or more photos don't belong to this listing.");

        if (listing.Images.Count - removed.Count + dto.Images.Count > MarketplaceLimits.MaxImagesPerListing)
            throw new BadRequestException($"A listing can have at most {MarketplaceLimits.MaxImagesPerListing} photos.");

        // New files are saved first so a rejected upload leaves the listing's current photos untouched.
        var added = await SaveImagesAsync(dto.Images);

        // Explicit Add/Delete: new photos arrive with a pre-generated Id, so EF would otherwise treat them as existing rows.
        foreach (var image in removed)
            _images.Delete(image);
        foreach (var image in added)
        {
            image.ListingId = listing.Id;
            await _images.AddAsync(image);
        }

        // Keep the order gapless: remaining photos first (in their old order), then the new ones.
        var order = 0;
        // EF's relationship fix-up already put the new photos into listing.Images, so they're excluded here.
        var remaining = listing.Images.Where(i => !removed.Contains(i) && !added.Contains(i)).OrderBy(i => i.SortOrder).ToList();
        foreach (var image in remaining.Concat(added))
            image.SortOrder = order++;

        listing.Title = dto.Title.Trim();
        listing.Price = dto.Price;
        listing.Category = dto.Category.Trim();
        listing.Location = NullIfBlank(dto.Location);
        listing.Description = NullIfBlank(dto.Description);

        await _listings.SaveChangesAsync();

        foreach (var image in removed)
            _files.Delete(image.Url);

        return (await BuildDtosAsync(new[] { listing }, currentUserId))[0];
    }

    public async Task<GetMarketplaceListingDto> SetStatusAsync(Guid currentUserId, Guid listingId, PutListingStatusDto dto)
    {
        if (!Enum.TryParse<ListingStatus>(dto.Status, out var status))
            throw new BadRequestException("Status must be either 'Active' or 'Sold'.");

        var listing = await GetOwnedListingAsync(currentUserId, listingId, "You can only change the status of your own listings.");

        var becomesSold = status == ListingStatus.Sold && listing.Status != ListingStatus.Sold;

        listing.Status = status;
        await _listings.SaveChangesAsync();

        // Selling it outside the offer flow ends every running negotiation on it.
        if (becomesSold)
            await _offerService.CloseOpenOffersAsync(currentUserId, listing.Id);

        return (await BuildDtosAsync(new[] { listing }, currentUserId))[0];
    }

    public async Task<GetMarketplaceListingDto> GetByIdAsync(Guid currentUserId, Guid listingId)
    {
        var listing = await _listings.GetByIdAsync(listingId, ListingIncludes)
            ?? throw new NotFoundException("Listing not found.");

        return (await BuildDtosAsync(new[] { listing }, currentUserId))[0];
    }

    public async Task<PagedResult<GetMarketplaceListingDto>> GetListingsAsync(Guid currentUserId, MarketplaceListingQuery query)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);

        var category = NullIfBlank(query.Category);
        var term = NullIfBlank(query.Search);
        var location = NullIfBlank(query.Location);

        var listings = _listings.GetAll(l => l.Status == ListingStatus.Active, asNoTracking: true, includes: ListingIncludes);

        if (category != null)
            listings = listings.Where(l => l.Category == category);

        if (term != null)
            listings = listings.Where(l => l.Title.Contains(term) || (l.Description != null && l.Description.Contains(term)));

        if (location != null)
            listings = listings.Where(l => l.Location != null && l.Location.Contains(location));

        if (query.SellerId.HasValue)
            listings = listings.Where(l => l.SellerId == query.SellerId.Value);

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
            Items = await BuildDtosAsync(items, currentUserId),
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
                asNoTracking: true,
                includes: ListingIncludes)
            .ToListAsync();

        return await BuildDtosAsync(listings, currentUserId);
    }

    public async Task<List<GetMarketplaceListingDto>> GetSavedListingsAsync(Guid currentUserId)
    {
        var entries = await _saved.GetAll(
                filter: s => s.UserId == currentUserId,
                orderBy: s => s.CreatedAt,
                isDescending: true,
                asNoTracking: true,
                includes: SavedListingIncludes)
            .ToListAsync();

        var listings = entries.Where(e => e.Listing != null).Select(e => e.Listing!).ToList();
        var dtos = await BuildDtosAsync(listings, currentUserId);
        dtos.ForEach(d => d.IsSavedByCurrentUser = true);
        return dtos;
    }

    public async Task<bool> ToggleSaveAsync(Guid currentUserId, Guid listingId)
    {
        var listing = await _listings.GetByIdAsync(listingId)
            ?? throw new NotFoundException("Listing not found.");

        if (listing.SellerId == currentUserId)
            throw new BadRequestException("You can't save your own listing.");

        var existing = await _saved.GetAll(s => s.UserId == currentUserId && s.ListingId == listingId).FirstOrDefaultAsync();

        if (existing != null)
            _saved.Delete(existing);
        else
            await _saved.AddAsync(new SavedListing { UserId = currentUserId, ListingId = listingId });

        await _saved.SaveChangesAsync();
        return existing == null;
    }

    public async Task DeleteAsync(Guid currentUserId, Guid listingId)
    {
        var listing = await GetOwnedListingAsync(currentUserId, listingId, "You can only delete your own listings.");
        var imageUrls = listing.Images.Select(i => i.Url).ToList();

        _listings.Delete(listing);
        await _listings.SaveChangesAsync();

        foreach (var url in imageUrls)
            _files.Delete(url);
    }

    private async Task<MarketplaceListing> GetOwnedListingAsync(Guid currentUserId, Guid listingId, string forbiddenMessage)
    {
        var listing = await _listings.GetByIdAsync(listingId, ListingIncludes)
            ?? throw new NotFoundException("Listing not found.");

        if (listing.SellerId != currentUserId)
            throw new ForbiddenException(forbiddenMessage);

        return listing;
    }

    // Saves the files in order; if one is rejected, the ones already written are removed again.
    private async Task<List<ListingImage>> SaveImagesAsync(IEnumerable<IFormFile> files)
    {
        var saved = new List<ListingImage>();
        try
        {
            foreach (var file in files)
            {
                var url = await _files.SaveImageAsync(file, "marketplace");
                saved.Add(new ListingImage { Url = url, SortOrder = saved.Count });
            }
        }
        catch
        {
            foreach (var image in saved)
                _files.Delete(image.Url);
            throw;
        }

        return saved;
    }

    private static string? NullIfBlank(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    // Fills in what AutoMapper can't: seller display info and the per-viewer "saved" flag (one batched query).
    private async Task<List<GetMarketplaceListingDto>> BuildDtosAsync(IEnumerable<MarketplaceListing> listings, Guid currentUserId)
    {
        var list = listings.ToList();
        var listingIds = list.Select(l => l.Id).ToList();
        var sellers = await _users.GetSummariesAsync(list.Select(l => l.SellerId));
        var savedIds = (await _saved.GetAll(s => s.UserId == currentUserId && listingIds.Contains(s.ListingId), asNoTracking: true)
            .Select(s => s.ListingId)
            .ToListAsync()).ToHashSet();

        // Running negotiations the viewer is part of (as buyer or as seller), again one batched query.
        var openOffers = await _offers.GetAll(
                o => o.Status == OfferStatus.Open && listingIds.Contains(o.ListingId)
                     && (o.BuyerId == currentUserId || o.SellerId == currentUserId),
                asNoTracking: true)
            .Select(o => new { o.Id, o.ListingId, o.BuyerId })
            .ToListAsync();

        // Average + count per distinct seller of this page, grouped in SQL (never one query per listing).
        var sellerIds = list.Select(l => l.SellerId).Distinct().ToList();
        var sellerRatings = (await _ratings.GetAll(r => sellerIds.Contains(r.SellerId), asNoTracking: true)
                .GroupBy(r => r.SellerId)
                .Select(g => new { SellerId = g.Key, Average = g.Average(r => (double)r.Stars), Count = g.Count() })
                .ToListAsync())
            .ToDictionary(x => x.SellerId);

        var dtos = _mapper.Map<List<GetMarketplaceListingDto>>(list);
        for (var i = 0; i < dtos.Count; i++)
        {
            if (sellerRatings.TryGetValue(list[i].SellerId, out var rated))
            {
                dtos[i].SellerRatingAverage = rated.Average;
                dtos[i].SellerRatingCount = rated.Count;
            }

            dtos[i].IsSavedByCurrentUser = savedIds.Contains(list[i].Id);
            dtos[i].MyOpenOfferId = openOffers.FirstOrDefault(o => o.ListingId == list[i].Id && o.BuyerId == currentUserId)?.Id;
            if (list[i].SellerId == currentUserId)
                dtos[i].OpenOfferCount = openOffers.Count(o => o.ListingId == list[i].Id);

            if (sellers.TryGetValue(list[i].SellerId, out var seller))
            {
                dtos[i].SellerName = seller.FullName;
                dtos[i].SellerAvatarUrl = seller.AvatarUrl;
            }
        }

        return dtos;
    }
}
