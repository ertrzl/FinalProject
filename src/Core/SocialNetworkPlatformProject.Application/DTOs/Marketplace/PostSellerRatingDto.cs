namespace SocialNetworkPlatformProject.Application.DTOs.Marketplace;

// "Satıcıyı Puanla" modal: 1-5 yellow stars and an optional short comment.
public class PostSellerRatingDto
{
    public int Stars { get; set; }
    public string? Comment { get; set; }
}
