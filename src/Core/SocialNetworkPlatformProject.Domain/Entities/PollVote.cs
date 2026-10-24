using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class PollVote : BaseEntity
{
    public Guid PollId { get; set; }

    public Guid OptionId { get; set; }
    public PollOption? Option { get; set; }

    public Guid UserId { get; set; }
}
