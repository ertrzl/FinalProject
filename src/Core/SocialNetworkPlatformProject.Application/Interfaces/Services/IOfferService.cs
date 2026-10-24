using SocialNetworkPlatformProject.Application.DTOs.Marketplace;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IOfferService
{
    Task<GetListingOfferDto> CreateAsync(Guid currentUserId, Guid listingId, PostOfferPriceDto dto);

    Task<List<GetListingOfferDto>> GetReceivedAsync(Guid currentUserId);

    Task<List<GetListingOfferDto>> GetSentAsync(Guid currentUserId);

    Task<GetListingOfferDto> CounterAsync(Guid currentUserId, Guid offerId, PostOfferPriceDto dto);

    Task<GetListingOfferDto> AcceptAsync(Guid currentUserId, Guid offerId);

    Task<GetListingOfferDto> RejectAsync(Guid currentUserId, Guid offerId);

    Task<GetListingOfferDto> WithdrawAsync(Guid currentUserId, Guid offerId);

    Task CloseOpenOffersAsync(Guid sellerId, Guid listingId);

    Task<bool> HasOpenOffersAsync(Guid listingId);
}
