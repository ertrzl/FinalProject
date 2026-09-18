using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// saved.html: "Kaydet" dropdown item on a post / "Kaydı Kaldır" button
public class SavedPost : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid PostId { get; set; }
    public Post? Post { get; set; }
}
