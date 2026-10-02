namespace SocialNetworkPlatformProject.Application.DTOs.Marketplace;

// One review in the seller's "Değerlendirmeler" list.
public class GetSellerRatingDto
{
    public Guid Id { get; set; }
    public Guid OfferId { get; set; }

    public Guid ReviewerId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public string? ReviewerAvatarUrl { get; set; }

    public int Stars { get; set; }
    public string? Comment { get; set; }
    public string ListingTitle { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// The rating a deal already has, shown on the negotiation card to both buyer and seller.
public class GetOfferRatingDto
{
    public int Stars { get; set; }
    public string? Comment { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Average + how many people gave each star count, for the reviews modal header and the profile badge.
public class GetSellerRatingSummaryDto
{
    public Guid SellerId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public string? SellerAvatarUrl { get; set; }

    public double? Average { get; set; } // null while there are no ratings yet
    public int Count { get; set; }
    public int[] Distribution { get; set; } = new int[5]; // index 0 = how many 1-star ratings ... index 4 = 5-star
}
