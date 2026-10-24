using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.DTOs.Marketplace;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class MarketplaceService : IMarketplaceService
{
    private static readonly string[] ListingIncludes = { "Images" };
    private static readonly string[] SavedListingIncludes = { "Listing.Images" };

    private readonly IMarketplaceListingRepository _listings;
    private readonly IListingImageRepository _images;
    private readonly ISavedListingRepository _saved;
    private readonly IListingOfferRepository _offers;
    private readonly IListingBoostRepository _boosts;
    private readonly ISellerRatingRepository _ratings;
    private readonly IOfferService _offerService;
    private readonly INotificationService _notifications;
    private readonly IMessageService _messages;
    private readonly IBlockService _blocks;
    private readonly IUserRepository _users;
    private readonly IFileStorageService _files;
    private readonly IMapper _mapper;

    public MarketplaceService(
        IMarketplaceListingRepository listings,
        IListingImageRepository images,
        ISavedListingRepository saved,
        IListingOfferRepository offers,
        IListingBoostRepository boosts,
        ISellerRatingRepository ratings,
        IOfferService offerService,
        INotificationService notifications,
        IMessageService messages,
        IBlockService blocks,
        IUserRepository users,
        IFileStorageService files,
        IMapper mapper)
    {
        _listings = listings;
        _images = images;
        _saved = saved;
        _offers = offers;
        _boosts = boosts;
        _ratings = ratings;
        _offerService = offerService;
        _notifications = notifications;
        _messages = messages;
        _blocks = blocks;
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
            Condition = ParseCondition(dto.Condition),
            Location = NullIfBlank(dto.Location),
            Description = NullIfBlank(dto.Description),
            ExpiresAt = DateTime.UtcNow.AddDays(MarketplaceLimits.ListingLifetimeDays)
        };

        foreach (var image in await SaveImagesAsync(dto.Images))
            listing.Images.Add(image);

        await _listings.AddAsync(listing);
        await _listings.SaveChangesAsync();

        return (await BuildDtosAsync(new[] { listing }, currentUserId))[0];
    }

    public async Task<GetMarketplaceListingDto> UpdateAsync(Guid currentUserId, Guid listingId, PutMarketplaceListingDto dto)
    {
        var listing = await GetOwnedListingAsync(currentUserId, listingId, Loc.T("listing.editOwn"));

        if (dto.Price != listing.Price && await _offerService.HasOpenOffersAsync(listing.Id))
            throw new BadRequestException(Loc.T("listing.priceLocked"));

        var removeIds = dto.RemoveImageIds.Distinct().ToList();
        var removed = listing.Images.Where(i => removeIds.Contains(i.Id)).ToList();
        if (removed.Count != removeIds.Count)
            throw new BadRequestException(Loc.T("listing.photosMismatch"));

        if (listing.Images.Count - removed.Count + dto.Images.Count > MarketplaceLimits.MaxImagesPerListing)
            throw new BadRequestException(Loc.T("listing.tooManyPhotos", MarketplaceLimits.MaxImagesPerListing));

        var added = await SaveImagesAsync(dto.Images);

        foreach (var image in removed)
            _images.Delete(image);
        foreach (var image in added)
        {
            image.ListingId = listing.Id;
            await _images.AddAsync(image);
        }

        var order = 0;
        var remaining = listing.Images.Where(i => !removed.Contains(i) && !added.Contains(i)).OrderBy(i => i.SortOrder).ToList();
        foreach (var image in remaining.Concat(added))
            image.SortOrder = order++;

        var previousPrice = listing.Price;

        listing.Title = dto.Title.Trim();
        listing.Price = dto.Price;
        listing.Category = dto.Category.Trim();
        listing.Condition = ParseCondition(dto.Condition);
        listing.Location = NullIfBlank(dto.Location);
        listing.Description = NullIfBlank(dto.Description);

        await _listings.SaveChangesAsync();

        foreach (var image in removed)
            _files.Delete(image.Url);

        if (listing.Price < previousPrice && listing.Status != ListingStatus.Sold && listing.ExpiresAt > DateTime.UtcNow)
            await NotifyPriceDropAsync(listing, previousPrice);

        return (await BuildDtosAsync(new[] { listing }, currentUserId))[0];
    }

    public async Task<GetMarketplaceListingDto> SetStatusAsync(Guid currentUserId, Guid listingId, PutListingStatusDto dto)
    {
        if (!Enum.TryParse<ListingStatus>(dto.Status, out var status) || !Enum.IsDefined(status))
            throw new BadRequestException(Loc.T("listing.status"));

        var listing = await GetOwnedListingAsync(currentUserId, listingId, Loc.T("listing.statusOwn"));

        var becomesSold = status == ListingStatus.Sold && listing.Status != ListingStatus.Sold;

        listing.Status = status;

        if (status == ListingStatus.Sold)
            listing.FeaturedUntil = null;

        await _listings.SaveChangesAsync();

        if (becomesSold)
            await _offerService.CloseOpenOffersAsync(currentUserId, listing.Id);

        return (await BuildDtosAsync(new[] { listing }, currentUserId))[0];
    }

    public async Task<GetMarketplaceListingDto> RenewAsync(Guid currentUserId, Guid listingId)
    {
        var listing = await GetOwnedListingAsync(currentUserId, listingId, Loc.T("listing.renewOwn"));
        var now = DateTime.UtcNow;

        if (listing.Status == ListingStatus.Sold)
            throw new BadRequestException(Loc.T("listing.renewSold"));

        if (!InRenewWindow(listing, now))
            throw new BadRequestException(Loc.T("listing.renewNotNeeded", MarketplaceLimits.RenewWindowDays));

        var nextRenewAt = listing.RenewedAt?.AddHours(MarketplaceLimits.RenewCooldownHours);
        if (nextRenewAt > now)
            throw new BadRequestException(Loc.T("listing.renewCooldown", WaitText(nextRenewAt.Value - now)));

        listing.ExpiresAt = now.AddDays(MarketplaceLimits.ListingLifetimeDays);
        listing.RenewedAt = now;
        await _listings.SaveChangesAsync();

        return (await BuildDtosAsync(new[] { listing }, currentUserId))[0];
    }

    public async Task<GetMarketplaceListingDto> FeatureAsync(Guid currentUserId, Guid listingId)
    {
        var listing = await GetOwnedListingAsync(currentUserId, listingId, Loc.T("listing.featureOwn"));
        var now = DateTime.UtcNow;

        if (listing.Status != ListingStatus.Active || listing.ExpiresAt <= now)
            throw new BadRequestException(Loc.T("listing.featureNotActive"));

        if (await _listings.HasFeaturedAsync(currentUserId, now))
            throw new BadRequestException(Loc.T("listing.featureRunning"));

        var nextFeatureAt = (await _boosts.GetLastBoostAtAsync(currentUserId))?.AddDays(MarketplaceLimits.FeatureCooldownDays);
        if (nextFeatureAt > now)
            throw new BadRequestException(Loc.T("listing.featureCooldown", WaitText(nextFeatureAt.Value - now)));

        listing.FeaturedUntil = now.AddHours(MarketplaceLimits.FeatureHours);
        await _boosts.AddAsync(new ListingBoost { SellerId = currentUserId });
        await _listings.SaveChangesAsync();

        return (await BuildDtosAsync(new[] { listing }, currentUserId))[0];
    }

    public async Task<GetMarketplaceListingDto> GetByIdAsync(Guid currentUserId, Guid listingId)
    {
        var listing = await _listings.GetByIdAsync(listingId, ListingIncludes)
            ?? throw new NotFoundException(Loc.T("listing.notFound"));

        return (await BuildDtosAsync(new[] { listing }, currentUserId, includeMessaging: true))[0];
    }

    public async Task<List<GetMarketplaceListingDto>> GetSimilarAsync(Guid currentUserId, Guid listingId)
    {
        var listing = await _listings.GetByIdAsync(listingId)
            ?? throw new NotFoundException(Loc.T("listing.notFound"));

        var hidden = await _blocks.GetBlockedIdsAsync(currentUserId);
        var similar = await _listings.GetSimilarAsync(listing, currentUserId, hidden, DateTime.UtcNow, MarketplaceLimits.MaxSimilarListings);

        return await BuildDtosAsync(similar, currentUserId);
    }

    public async Task<PagedResult<GetMarketplaceListingDto>> GetListingsAsync(Guid currentUserId, MarketplaceListingQuery query)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 50);
        var now = DateTime.UtcNow;

        var category = NullIfBlank(query.Category);
        var term = NullIfBlank(query.Search);
        var location = NullIfBlank(query.Location);
        var includeSold = query.IncludeSold;

        var listings = _listings.GetAll(
            l => l.Status == ListingStatus.Sold ? includeSold : l.ExpiresAt > now,
            asNoTracking: true,
            includes: ListingIncludes);

        if (category != null)
            listings = listings.Where(l => l.Category == category);

        if (MarketplaceConditions.TryParse(NullIfBlank(query.Condition), out var condition))
            listings = listings.Where(l => l.Condition == condition);

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

        var ordered = query.Sort switch
        {
            MarketplaceSort.PriceAsc => listings.OrderBy(l => l.Price).ThenByDescending(l => l.CreatedAt).ThenBy(l => l.Id),
            MarketplaceSort.PriceDesc => listings.OrderByDescending(l => l.Price).ThenByDescending(l => l.CreatedAt).ThenBy(l => l.Id),
            MarketplaceSort.Nearby => await OrderNearbyAsync(listings, currentUserId),
            _ => listings
                .OrderByDescending(l => l.Status == ListingStatus.Active && l.FeaturedUntil != null && l.FeaturedUntil > now)
                .ThenByDescending(l => l.CreatedAt)
                .ThenBy(l => l.Id)
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

    private async Task<IOrderedQueryable<MarketplaceListing>> OrderNearbyAsync(IQueryable<MarketplaceListing> listings, Guid currentUserId)
    {
        var parts = (await _users.GetLocationAsync(currentUserId))?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Array.Empty<string>();

        if (parts.Length == 0)
            return listings.OrderByDescending(l => l.CreatedAt).ThenBy(l => l.Id);

        var city = parts[0];
        var region = parts[^1];

        return listings
            .OrderBy(l => l.Location != null && l.Location.Contains(city) ? 0 : (l.Location != null && l.Location.Contains(region) ? 1 : 2))
            .ThenByDescending(l => l.CreatedAt)
            .ThenBy(l => l.Id);
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
            ?? throw new NotFoundException(Loc.T("listing.notFound"));

        if (listing.SellerId == currentUserId)
            throw new BadRequestException(Loc.T("listing.saveOwn"));

        var existing = await _saved.GetAll(s => s.UserId == currentUserId && s.ListingId == listingId).FirstOrDefaultAsync();

        if (existing != null)
            _saved.Delete(existing);
        else
            await _saved.AddAsync(new SavedListing { UserId = currentUserId, ListingId = listingId });

        await _saved.SaveChangesAsync();
        return existing == null;
    }

    public async Task DeleteAsync(Guid currentUserId, Guid listingId, bool asAdmin = false)
    {
        var listing = await GetOwnedListingAsync(currentUserId, listingId, Loc.T("listing.deleteOwn"), asAdmin);
        var imageUrls = listing.Images.Select(i => i.Url).ToList();

        _listings.Delete(listing);
        await _listings.SaveChangesAsync();

        foreach (var url in imageUrls)
            _files.Delete(url);

        await _notifications.DeleteByListingAsync(listingId);
    }

    private async Task<MarketplaceListing> GetOwnedListingAsync(Guid currentUserId, Guid listingId, string forbiddenMessage, bool asAdmin = false)
    {
        var listing = await _listings.GetByIdAsync(listingId, ListingIncludes)
            ?? throw new NotFoundException(Loc.T("listing.notFound"));

        if (!asAdmin && listing.SellerId != currentUserId)
            throw new ForbiddenException(forbiddenMessage);

        return listing;
    }

    private async Task NotifyPriceDropAsync(MarketplaceListing listing, decimal previousPrice)
    {
        foreach (var userId in await _saved.GetSaverIdsAsync(listing.Id))
        {
            await _notifications.CreateAsync(userId, listing.SellerId, NotificationType.MarketplacePriceDropped,
                subject: listing.Title, amount: listing.Price, previousAmount: previousPrice, listingId: listing.Id);
        }
    }

    private static bool InRenewWindow(MarketplaceListing listing, DateTime now)
    {
        return listing.ExpiresAt - now <= TimeSpan.FromDays(MarketplaceLimits.RenewWindowDays);
    }

    private static string WaitText(TimeSpan wait)
    {
        if (wait.TotalHours < 1)
            return Loc.T("listing.wait.minutes", Math.Max(1, (int)Math.Ceiling(wait.TotalMinutes)));

        if (wait.TotalHours < 48)
            return Loc.T("listing.wait.hours", (int)Math.Ceiling(wait.TotalHours));

        return Loc.T("listing.wait.days", (int)Math.Ceiling(wait.TotalDays));
    }

    private static ListingCondition ParseCondition(string value)
    {
        return MarketplaceConditions.TryParse(value, out var condition)
            ? condition
            : throw new BadRequestException(Loc.T("val.listing.condition"));
    }

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

    private async Task<List<GetMarketplaceListingDto>> BuildDtosAsync(IEnumerable<MarketplaceListing> listings, Guid currentUserId, bool includeMessaging = false)
    {
        var list = listings.ToList();
        var now = DateTime.UtcNow;
        var listingIds = list.Select(l => l.Id).ToList();
        var sellers = await _users.GetSummariesAsync(list.Select(l => l.SellerId));
        var savedIds = (await _saved.GetAll(s => s.UserId == currentUserId && listingIds.Contains(s.ListingId), asNoTracking: true)
            .Select(s => s.ListingId)
            .ToListAsync()).ToHashSet();

        var openOffers = await _offers.GetAll(
                o => o.Status == OfferStatus.Open && listingIds.Contains(o.ListingId)
                     && (o.BuyerId == currentUserId || o.SellerId == currentUserId),
                asNoTracking: true)
            .Select(o => new { o.Id, o.ListingId, o.BuyerId })
            .ToListAsync();

        var sellerIds = list.Select(l => l.SellerId).Distinct().ToList();
        var sellerRatings = (await _ratings.GetAll(r => sellerIds.Contains(r.SellerId), asNoTracking: true)
                .GroupBy(r => r.SellerId)
                .Select(g => new { SellerId = g.Key, Average = g.Average(r => (double)r.Stars), Count = g.Count() })
                .ToListAsync())
            .ToDictionary(x => x.SellerId);

        var hasOwn = list.Any(l => l.SellerId == currentUserId);
        var hasFeatured = hasOwn && await _listings.HasFeaturedAsync(currentUserId, now);
        var nextFeatureAt = hasOwn
            ? (await _boosts.GetLastBoostAtAsync(currentUserId))?.AddDays(MarketplaceLimits.FeatureCooldownDays)
            : null;

        var canMessage = new Dictionary<Guid, bool>();
        if (includeMessaging)
        {
            foreach (var sellerId in sellerIds.Where(id => id != currentUserId))
                canMessage[sellerId] = await _messages.CanMessageAsync(currentUserId, sellerId);
        }

        var dtos = _mapper.Map<List<GetMarketplaceListingDto>>(list);
        for (var i = 0; i < dtos.Count; i++)
        {
            var listing = list[i];

            if (sellerRatings.TryGetValue(listing.SellerId, out var rated))
            {
                dtos[i].SellerRatingAverage = rated.Average;
                dtos[i].SellerRatingCount = rated.Count;
            }

            dtos[i].IsExpired = listing.Status != ListingStatus.Sold && listing.ExpiresAt <= now;
            dtos[i].IsFeatured = listing.Status == ListingStatus.Active && !dtos[i].IsExpired
                && listing.FeaturedUntil != null && listing.FeaturedUntil > now;
            dtos[i].CanMessageSeller = canMessage.TryGetValue(listing.SellerId, out var allowed) && allowed;

            dtos[i].IsSavedByCurrentUser = savedIds.Contains(listing.Id);
            dtos[i].MyOpenOfferId = openOffers.FirstOrDefault(o => o.ListingId == listing.Id && o.BuyerId == currentUserId)?.Id;

            if (listing.SellerId == currentUserId)
            {
                dtos[i].OpenOfferCount = openOffers.Count(o => o.ListingId == listing.Id);
                FillOwnerLimits(dtos[i], listing, now, hasFeatured, nextFeatureAt);
            }

            if (sellers.TryGetValue(listing.SellerId, out var seller))
            {
                dtos[i].SellerName = seller.FullName;
                dtos[i].SellerAvatarUrl = seller.AvatarUrl;
            }
        }

        return dtos;
    }

    private static void FillOwnerLimits(GetMarketplaceListingDto dto, MarketplaceListing listing, DateTime now, bool hasFeatured, DateTime? nextFeatureAt)
    {
        if (listing.Status != ListingStatus.Sold && InRenewWindow(listing, now))
        {
            var nextRenewAt = listing.RenewedAt?.AddHours(MarketplaceLimits.RenewCooldownHours);
            dto.CanRenew = !(nextRenewAt > now);
            dto.NextRenewAt = nextRenewAt > now ? nextRenewAt : null;
        }

        if (listing.Status == ListingStatus.Active && listing.ExpiresAt > now)
        {
            dto.CanFeature = !hasFeatured && !(nextFeatureAt > now);
            dto.NextFeatureAt = nextFeatureAt > now ? nextFeatureAt : null;
        }
    }
}
