namespace SocialNetworkPlatformProject.Application.DTOs.Marketplace;

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

public class GetOfferRatingDto
{
    public int Stars { get; set; }
    public string? Comment { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class GetSellerRatingSummaryDto
{
    public Guid SellerId { get; set; }
    public string SellerName { get; set; } = string.Empty;
    public string? SellerAvatarUrl { get; set; }

    public double? Average { get; set; }
    public int Count { get; set; }
    public int[] Distribution { get; set; } = new int[5];
}
