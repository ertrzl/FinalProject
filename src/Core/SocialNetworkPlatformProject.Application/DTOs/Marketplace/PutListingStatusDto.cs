namespace SocialNetworkPlatformProject.Application.DTOs.Marketplace;

// "Satıldı olarak işaretle" / "Tekrar satışa çıkar" buttons in the listing detail modal.
public class PutListingStatusDto
{
    public string Status { get; set; } = "Active"; // "Active" / "Sold"
}
