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
    // The DTO shows the listing's cover and the whole price history, so both come along with every offer.
    private static readonly string[] OfferIncludes = { "Rounds", "Listing.Images" };

    private readonly IListingOfferRepository _offers;
    private readonly IListingOfferRoundRepository _rounds;
    private readonly IMarketplaceListingRepository _listings;
    private readonly ISellerRatingRepository _ratings;
    private readonly IUserRepository _users;
    private readonly INotificationService _notifications;

    public OfferService(
        IListingOfferRepository offers,
        IListingOfferRoundRepository rounds,
        IMarketplaceListingRepository listings,
        ISellerRatingRepository ratings,
        IUserRepository users,
        INotificationService notifications)
    {
        _offers = offers;
        _rounds = rounds;
        _listings = listings;
        _ratings = ratings;
        _users = users;
        _notifications = notifications;
    }

    public async Task<GetListingOfferDto> CreateAsync(Guid currentUserId, Guid listingId, PostOfferPriceDto dto)
    {
        var listing = await _listings.GetByIdAsync(listingId)
            ?? throw new NotFoundException("Listing not found.");

        if (listing.SellerId == currentUserId)
            throw new BadRequestException("You can't make an offer on your own listing.");

        if (listing.Status != ListingStatus.Active)
            throw new BadRequestException("This listing is no longer for sale.");

        if (dto.Price > listing.Price)
            throw new BadRequestException($"An offer can't be higher than the asking price ({Format(listing.Price)}).");

        if (await _offers.AnyAsync(o => o.ListingId == listingId && o.BuyerId == currentUserId && o.Status == OfferStatus.Open))
            throw new ConflictException("You already have a running offer on this listing.");

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
        var offers = await _offers.GetAll(
                filter: o => o.SellerId == currentUserId,
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
        var offers = await _offers.GetAll(
                filter: o => o.BuyerId == currentUserId,
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

        // The price has to move toward the other side, but never all the way: strictly between the price
        // they just proposed and the last one I proposed (the asking price, for the seller's first counter).
        var ownPrevious = offer.Rounds
            .Where(r => r.ProposerId == currentUserId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => (decimal?)r.Price)
            .FirstOrDefault() ?? offer.Listing!.Price;

        var low = Math.Min(offer.CurrentPrice, ownPrevious);
        var high = Math.Max(offer.CurrentPrice, ownPrevious);
        if (dto.Price <= low || dto.Price >= high)
            throw new BadRequestException($"A counter-offer must be between {Format(low)} and {Format(high)}.");

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
            throw new BadRequestException("You can only withdraw your own proposal. Answer the other side's proposal instead.");

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

    // ---- helpers ----

    // A tracked offer (with rounds and listing) that the current user takes part in. Strangers get a 403.
    private async Task<ListingOffer> LoadParticipantOfferAsync(Guid currentUserId, Guid offerId)
    {
        var offer = await _offers.GetAll(o => o.Id == offerId, includes: OfferIncludes).AsSplitQuery().FirstOrDefaultAsync()
            ?? throw new NotFoundException("Offer not found.");

        if (offer.BuyerId != currentUserId && offer.SellerId != currentUserId)
            throw new ForbiddenException("This offer isn't yours.");

        return offer;
    }

    // Accept / counter / reject all need an open negotiation, a still-for-sale listing and the user to be the
    // one who has to answer (i.e. not the author of the latest proposal).
    private async Task<ListingOffer> LoadAnswerableOfferAsync(Guid currentUserId, Guid offerId)
    {
        var offer = await LoadParticipantOfferAsync(currentUserId, offerId);
        EnsureOpen(offer);

        if (offer.LastProposerId == currentUserId)
            throw new BadRequestException("It's the other side's turn to answer your proposal.");

        if (offer.Listing!.Status != ListingStatus.Active)
            throw new BadRequestException("This listing is no longer for sale.");

        return offer;
    }

    private static void EnsureOpen(ListingOffer offer)
    {
        if (offer.Status != OfferStatus.Open)
            throw new ConflictException("This negotiation has already ended.");
    }

    private static Guid OtherParty(ListingOffer offer, Guid currentUserId)
    {
        return offer.BuyerId == currentUserId ? offer.SellerId : offer.BuyerId;
    }

    private static string Format(decimal price)
    {
        return price.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private async Task<GetListingOfferDto> GetDtoAsync(Guid offerId, Guid currentUserId)
    {
        var offer = await _offers.GetAll(o => o.Id == offerId, asNoTracking: true, includes: OfferIncludes).AsSplitQuery().FirstAsync();
        return (await BuildDtosAsync(new[] { offer }, currentUserId))[0];
    }

    // Names/avatars live on the Identity users, so they're resolved here in one batched lookup.
    private async Task<List<GetListingOfferDto>> BuildDtosAsync(IEnumerable<ListingOffer> offers, Guid currentUserId)
    {
        var list = offers.ToList();
        var people = await _users.GetSummariesAsync(list.SelectMany(o => new[] { o.BuyerId, o.SellerId }));

        // Only accepted deals can carry a rating: one batched lookup for the whole inbox.
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
