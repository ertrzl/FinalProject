using SocialNetworkPlatformProject.Application.DTOs.Marketplace;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// Counter-offer bargaining on marketplace listings. Every negotiation is turn-based: only the party who did
// not make the latest proposal may accept, counter or reject it; the proposer may withdraw it.
public interface IOfferService
{
    // Buyer opens a negotiation with an opening price (at most the asking price).
    Task<GetListingOfferDto> CreateAsync(Guid currentUserId, Guid listingId, PostOfferPriceDto dto);

    // "Gelen teklifler": negotiations on the current user's own listings.
    Task<List<GetListingOfferDto>> GetReceivedAsync(Guid currentUserId);

    // "Verdiğim teklifler": negotiations the current user started as a buyer.
    Task<List<GetListingOfferDto>> GetSentAsync(Guid currentUserId);

    // A counter must land strictly between the other side's last price and the proposer's own previous price.
    Task<GetListingOfferDto> CounterAsync(Guid currentUserId, Guid offerId, PostOfferPriceDto dto);

    // Takes the other side's latest price: the listing becomes Sold and every other open negotiation on it is closed.
    Task<GetListingOfferDto> AcceptAsync(Guid currentUserId, Guid offerId);

    Task<GetListingOfferDto> RejectAsync(Guid currentUserId, Guid offerId);

    Task<GetListingOfferDto> WithdrawAsync(Guid currentUserId, Guid offerId);

    // Used when a listing is marked Sold by its seller: closes every open negotiation on it and tells the buyers.
    Task CloseOpenOffersAsync(Guid sellerId, Guid listingId);

    Task<bool> HasOpenOffersAsync(Guid listingId);
}
