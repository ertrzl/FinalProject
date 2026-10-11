using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// A poll attached to a post (the post's text is the question). Everybody who can see the post may vote once, until
// EndsAt. Deleting the post deletes the poll with its options and votes.
public class Poll : BaseEntity
{
    public Guid PostId { get; set; }
    public Post? Post { get; set; }

    public DateTime EndsAt { get; set; } // UTC

    public ICollection<PollOption> Options { get; set; } = new List<PollOption>();

    public bool IsEnded(DateTime nowUtc) => EndsAt <= nowUtc;
}
