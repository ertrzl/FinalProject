using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class Poll : BaseEntity
{
    public Guid PostId { get; set; }
    public Post? Post { get; set; }

    public DateTime EndsAt { get; set; }

    public ICollection<PollOption> Options { get; set; } = new List<PollOption>();

    public bool IsEnded(DateTime nowUtc) => EndsAt <= nowUtc;
}
