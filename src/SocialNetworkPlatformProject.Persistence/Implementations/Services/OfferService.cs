using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SocialNetworkPlatformProject.Application.DTOs.Marketplace;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Repositories;
using SocialNetworkPlatformProject.Application.Interfaces.Services;
using SocialNetworkPlatformProject.Domain.Entities;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Persistence.Implementations.Services;

public class OfferService : IOfferService
{
    private static readonly string[] OfferIncludes = { "Rounds", "Listing.Images" };

    private readonly IListingOfferRepository _offers;
    private readonly IListingOfferRoundRepository _rounds;
    private readonly IMarketplaceListingRepository _listings;
    private readonly ISellerRatingRepository _ratings;
    private readonly IUserRepository _users;
    private readonly INotificationService _notifications;
    private readonly IBlockService _blocks;

    public OfferService(
        IListingOfferRepository offers,
        IListingOfferRoundRepository rounds,
        IMarketplaceListingRepository listings,
        ISellerRatingRepository ratings,
        IUserRepository users,
        INotificationService notifications,
        IBlockService blocks)
    {
        _offers = offers;
        _rounds = rounds;
        _listings = listings;
        _ratings = ratings;
        _users = users;
        _notifications = notifications;
        _blocks = blocks;
    }

    public async Task<GetListingOfferDto> CreateAsync(Guid currentUserId, Guid listingId, PostOfferPriceDto dto)
    {
        var listing = await _listings.GetByIdAsync(listingId)
            ?? throw new NotFoundException(Loc.T("listing.notFound"));

        if (listing.SellerId == currentUserId)
            throw new BadRequestException(Loc.T("offer.ownListing"));

        if (await _blocks.AreBlockedAsync(currentUserId, listing.SellerId))
            throw new ForbiddenException(Loc.T("block.cannotOffer"));

        if (listing.Status == ListingStatus.Sold)
            throw new BadRequestException(Loc.T("offer.notForSale"));

        if (listing.Status == ListingStatus.Reserved)
            throw new BadRequestException(Loc.T("offer.reserved"));

        if (listing.ExpiresAt <= DateTime.UtcNow)
            throw new BadRequestException(Loc.T("offer.expired"));

        if (dto.Price > listing.Price)
            throw new BadRequestException(Loc.T("offer.tooHigh", MarketplaceCurrencies.Format(listing.Price, listing.Currency)));

        if (await _offers.AnyAsync(o => o.ListingId == listingId && o.BuyerId == currentUserId && o.Status == OfferStatus.Open))
            throw new ConflictException(Loc.T("offer.running"));

        var offer = new ListingOffer
        {
            ListingId = listingId,
            BuyerId = currentUserId,
            SellerId = listing.SellerId,
            CurrentPrice = dto.Price,
            LastProposerId = currentUserId
        };
        offer.Rounds.Add(new ListingOfferRound { ProposerId = currentUserId, Price = dto.Price });

        await _offers.AddAsync(offer);
        await _offers.SaveChangesAsync();

        await _notifications.CreateAsync(listing.SellerId, currentUserId, NotificationType.MarketplaceOfferReceived,
            offerId: offer.Id, amount: dto.Price);

        return await GetDtoAsync(offer.Id, currentUserId);
    }

    public async Task<List<GetListingOfferDto>> GetReceivedAsync(Guid currentUserId)
    {
        var hidden = await _blocks.GetBlockedIdsAsync(currentUserId);

        var offers = await _offers.GetAll(
                filter: o => o.SellerId == currentUserId && !hidden.Contains(o.BuyerId),
                orderBy: o => o.UpdatedAt,
                isDescending: true,
                asNoTracking: true,
                includes: OfferIncludes)
            .AsSplitQuery()
            .ToListAsync();

        return await BuildDtosAsync(offers, currentUserId);
    }

    public async Task<List<GetListingOfferDto>> GetSentAsync(Guid currentUserId)
    {
        var hidden = await _blocks.GetBlockedIdsAsync(currentUserId);

        var offers = await _offers.GetAll(
                filter: o => o.BuyerId == currentUserId && !hidden.Contains(o.SellerId),
                orderBy: o => o.UpdatedAt,
                isDescending: true,
                asNoTracking: true,
                includes: OfferIncludes)
            .AsSplitQuery()
            .ToListAsync();

        return await BuildDtosAsync(offers, currentUserId);
    }

    public async Task<GetListingOfferDto> CounterAsync(Guid currentUserId, Guid offerId, PostOfferPriceDto dto)
    {
        var offer = await LoadAnswerableOfferAsync(currentUserId, offerId);

        var ownPrevious = offer.Rounds
            .Where(r => r.ProposerId == currentUserId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => (decimal?)r.Price)
            .FirstOrDefault() ?? offer.Listing!.Price;

        var low = Math.Min(offer.CurrentPrice, ownPrevious);
        var high = Math.Max(offer.CurrentPrice, ownPrevious);
        if (dto.Price <= low || dto.Price >= high)
            throw new BadRequestException(Loc.T("offer.counterRange", MarketplaceCurrencies.Format(low, offer.Listing!.Currency), MarketplaceCurrencies.Format(high, offer.Listing!.Currency)));

        await _rounds.AddAsync(new ListingOfferRound { OfferId = offer.Id, ProposerId = currentUserId, Price = dto.Price });
        offer.CurrentPrice = dto.Price;
        offer.LastProposerId = currentUserId;
        offer.UpdatedAt = DateTime.UtcNow;
        await _offers.SaveChangesAsync();

        await _notifications.CreateAsync(OtherParty(offer, currentUserId), currentUserId,
            NotificationType.MarketplaceOfferCountered, offerId: offer.Id, amount: dto.Price);

        return await GetDtoAsync(offer.Id, currentUserId);
    }

    public async Task<GetListingOfferDto> AcceptAsync(Guid currentUserId, Guid offerId)
    {
        var offer = await LoadAnswerableOfferAsync(currentUserId, offerId);

        offer.Status = OfferStatus.Accepted;
        offer.UpdatedAt = DateTime.UtcNow;
        offer.Listing!.Status = ListingStatus.Sold;

        var competing = await _offers.GetAll(o => o.ListingId == offer.ListingId && o.Id != offer.Id && o.Status == OfferStatus.Open).ToListAsync();
        foreach (var other in competing)
        {
            other.Status = OfferStatus.Closed;
            other.UpdatedAt = DateTime.UtcNow;
        }

        await _offers.SaveChangesAsync();

        await _notifications.CreateAsync(OtherParty(offer, currentUserId), currentUserId,
            NotificationType.MarketplaceOfferAccepted, offerId: offer.Id, amount: offer.CurrentPrice);

        foreach (var other in competing)
            await _notifications.CreateAsync(other.BuyerId, offer.SellerId, NotificationType.MarketplaceOfferClosed,
                offerId: other.Id, amount: other.CurrentPrice);

        return await GetDtoAsync(offer.Id, currentUserId);
    }

    public async Task<GetListingOfferDto> RejectAsync(Guid currentUserId, Guid offerId)
    {
        var offer = await LoadAnswerableOfferAsync(currentUserId, offerId);

        offer.Status = OfferStatus.Rejected;
        offer.UpdatedAt = DateTime.UtcNow;
        await _offers.SaveChangesAsync();

        await _notifications.CreateAsync(OtherParty(offer, currentUserId), currentUserId,
            NotificationType.MarketplaceOfferRejected, offerId: offer.Id, amount: offer.CurrentPrice);

        return await GetDtoAsync(offer.Id, currentUserId);
    }

    public async Task<GetListingOfferDto> WithdrawAsync(Guid currentUserId, Guid offerId)
    {
        var offer = await LoadParticipantOfferAsync(currentUserId, offerId);
        EnsureOpen(offer);

        if (offer.LastProposerId != currentUserId)
            throw new BadRequestException(Loc.T("offer.withdrawOwn"));

        offer.Status = OfferStatus.Withdrawn;
        offer.UpdatedAt = DateTime.UtcNow;
        await _offers.SaveChangesAsync();

        await _notifications.CreateAsync(OtherParty(offer, currentUserId), currentUserId,
            NotificationType.MarketplaceOfferWithdrawn, offerId: offer.Id, amount: offer.CurrentPrice);

        return await GetDtoAsync(offer.Id, currentUserId);
    }

    public async Task CloseOpenOffersAsync(Guid sellerId, Guid listingId)
    {
        var open = await _offers.GetAll(o => o.ListingId == listingId && o.Status == OfferStatus.Open).ToListAsync();
        if (open.Count == 0)
            return;

        foreach (var offer in open)
        {
            offer.Status = OfferStatus.Closed;
            offer.UpdatedAt = DateTime.UtcNow;
        }

        await _offers.SaveChangesAsync();

        foreach (var offer in open)
            await _notifications.CreateAsync(offer.BuyerId, sellerId, NotificationType.MarketplaceOfferClosed,
                offerId: offer.Id, amount: offer.CurrentPrice);
    }

    public async Task<bool> HasOpenOffersAsync(Guid listingId)
    {
        return await _offers.AnyAsync(o => o.ListingId == listingId && o.Status == OfferStatus.Open);
    }


    private async Task<ListingOffer> LoadParticipantOfferAsync(Guid currentUserId, Guid offerId)
    {
        var offer = await _offers.GetAll(o => o.Id == offerId, includes: OfferIncludes).AsSplitQuery().FirstOrDefaultAsync()
            ?? throw new NotFoundException(Loc.T("offer.notFound"));

        if (offer.BuyerId != currentUserId && offer.SellerId != currentUserId)
            throw new ForbiddenException(Loc.T("offer.notYours"));

        return offer;
    }

    private async Task<ListingOffer> LoadAnswerableOfferAsync(Guid currentUserId, Guid offerId)
    {
        var offer = await LoadParticipantOfferAsync(currentUserId, offerId);
        EnsureOpen(offer);

        if (await _blocks.AreBlockedAsync(offer.BuyerId, offer.SellerId))
            throw new ForbiddenException(Loc.T("block.cannotOffer"));

        if (offer.LastProposerId == currentUserId)
            throw new BadRequestException(Loc.T("offer.otherTurn"));

        if (offer.Listing!.Status == ListingStatus.Sold)
            throw new BadRequestException(Loc.T("offer.notForSale"));

        return offer;
    }

    private static void EnsureOpen(ListingOffer offer)
    {
        if (offer.Status != OfferStatus.Open)
            throw new ConflictException(Loc.T("offer.ended"));
    }

    private static Guid OtherParty(ListingOffer offer, Guid currentUserId)
    {
        return offer.BuyerId == currentUserId ? offer.SellerId : offer.BuyerId;
    }

    private async Task<GetListingOfferDto> GetDtoAsync(Guid offerId, Guid currentUserId)
    {
        var offer = await _offers.GetAll(o => o.Id == offerId, asNoTracking: true, includes: OfferIncludes).AsSplitQuery().FirstAsync();
        return (await BuildDtosAsync(new[] { offer }, currentUserId))[0];
    }

    private async Task<List<GetListingOfferDto>> BuildDtosAsync(IEnumerable<ListingOffer> offers, Guid currentUserId)
    {
        var list = offers.ToList();
        var people = await _users.GetSummariesAsync(list.SelectMany(o => new[] { o.BuyerId, o.SellerId }));

        var acceptedIds = list.Where(o => o.Status == OfferStatus.Accepted).Select(o => o.Id).ToList();
        var ratings = acceptedIds.Count == 0
            ? new Dictionary<Guid, SellerRating>()
            : (await _ratings.GetAll(r => acceptedIds.Contains(r.OfferId), asNoTracking: true).ToListAsync())
                .ToDictionary(r => r.OfferId);

        return list.Select(o =>
        {
            people.TryGetValue(o.BuyerId, out var buyer);
            people.TryGetValue(o.SellerId, out var seller);
            var isOpen = o.Status == OfferStatus.Open;

            return new GetListingOfferDto
            {
                Id = o.Id,
                ListingId = o.ListingId,
                ListingTitle = o.Listing?.Title ?? string.Empty,
                ListingImageUrl = o.Listing?.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault(),
                ListingPrice = o.Listing?.Price ?? 0,
                Currency = o.Listing?.Currency ?? MarketplaceCurrencies.Default,
                ListingStatus = (o.Listing?.Status ?? ListingStatus.Active).ToString(),
                BuyerId = o.BuyerId,
                BuyerName = buyer?.FullName ?? string.Empty,
                BuyerAvatarUrl = buyer?.AvatarUrl,
                SellerId = o.SellerId,
                SellerName = seller?.FullName ?? string.Empty,
                SellerAvatarUrl = seller?.AvatarUrl,
                Status = o.Status.ToString(),
                CurrentPrice = o.CurrentPrice,
                LastProposerId = o.LastProposerId,
                IsCurrentUserBuyer = o.BuyerId == currentUserId,
                IsMyTurn = isOpen && o.LastProposerId != currentUserId,
                CanWithdraw = isOpen && o.LastProposerId == currentUserId,
                Rating = ratings.TryGetValue(o.Id, out var rating)
                    ? new GetOfferRatingDto { Stars = rating.Stars, Comment = rating.Comment, UpdatedAt = rating.UpdatedAt }
                    : null,
                Rounds = o.Rounds
                    .OrderBy(r => r.CreatedAt)
                    .Select(r => new GetOfferRoundDto { ProposerId = r.ProposerId, Price = r.Price, CreatedAt = r.CreatedAt })
                    .ToList(),
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt
            };
        }).ToList();
    }
}
