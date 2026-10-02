namespace SocialNetworkPlatformProject.Application.DTOs.Marketplace;

// "Teklif Ver" (opens a negotiation) and "Karşı Teklif" (answers one) both carry just a price.
public class PostOfferPriceDto
{
    public decimal Price { get; set; }
}
