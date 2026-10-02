namespace SocialNetworkPlatformProject.Application.DTOs.Marketplace;

// marketplace.html "Tekliflerim" tab: one negotiation thread, seen from the current user's side.
public class GetListingOfferDto
{
    public Guid Id { get; set; }

    public Guid ListingId { get; set; }
    public string ListingTitle { get; set; } = string.Empty;
    public string? ListingImageUrl { get; set; }
    public decimal ListingPrice { get; set; } // the asking price
    public string ListingStatus { get; set; } = string.Empty; // "Active" / "Sold"

    public Guid BuyerId { get; set; }
    public string BuyerName { get; set; } = string.Empty;
    public string? BuyerAvatarUrl { get; set; }
    public Guid SellerId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public string? SellerAvatarUrl { get; set; }

    public string Status { get; set; } = string.Empty; // "Open" / "Accepted" / "Rejected" / "Withdrawn" / "Closed"
    public decimal CurrentPrice { get; set; }
    public Guid LastProposerId { get; set; }

    public bool IsCurrentUserBuyer { get; set; }
    public bool IsMyTurn { get; set; }    // open, and the last proposal came from the other side: accept / counter / reject
    public bool CanWithdraw { get; set; } // open, and the last proposal is mine
    public GetOfferRatingDto? Rating { get; set; } // the buyer's rating of this deal, once given

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
