namespace SocialNetworkPlatformProject.Application.DTOs.Marketplace;

public class GetListingOfferDto
{
    public Guid Id { get; set; }

    public Guid ListingId { get; set; }
    public string ListingTitle { get; set; } = string.Empty;
    public string? ListingImageUrl { get; set; }
    public decimal ListingPrice { get; set; }
    public string Currency { get; set; } = MarketplaceCurrencies.Default;
    public string ListingStatus { get; set; } = string.Empty;

    public Guid BuyerId { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string? BuyerAvatarUrl { get; set; }
    public Guid SellerId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public string? SellerAvatarUrl { get; set; }

    public string Status { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public Guid LastProposerId { get; set; }

    public bool IsCurrentUserBuyer { get; set; }
    public bool IsMyTurn { get; set; }
    public bool CanWithdraw { get; set; }
    public GetOfferRatingDto? Rating { get; set; }

    public List<GetOfferRoundDto> Rounds { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class GetOfferRoundDto
{
    public Guid ProposerId { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
}
