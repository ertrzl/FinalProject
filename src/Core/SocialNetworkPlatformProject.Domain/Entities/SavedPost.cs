using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class SavedPost : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid PostId { get; set; }
    public Post? Post { get; set; }
}
