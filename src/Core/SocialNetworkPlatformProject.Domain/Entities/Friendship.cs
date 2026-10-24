using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class Friendship : BaseEntity
{
    public Guid UserOneId { get; set; }
    public Guid UserTwoId { get; set; }
}
