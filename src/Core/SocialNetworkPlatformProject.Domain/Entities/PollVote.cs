using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// One person's vote in one poll. PollId is kept next to OptionId only so that "one vote per person and poll" can be
// a unique index; it has no foreign key of its own (a second cascade path to the same row is not allowed), the
// option's cascade removes the votes when the poll goes.
public class PollVote : BaseEntity
{
    public Guid PollId { get; set; }

    public Guid OptionId { get; set; }
    public PollOption? Option { get; set; }

    public Guid UserId { get; set; }
}
