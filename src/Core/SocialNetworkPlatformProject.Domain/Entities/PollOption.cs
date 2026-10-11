using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class PollOption : BaseEntity
{
    public Guid PollId { get; set; }
    public Poll? Poll { get; set; }

    public string Text { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
