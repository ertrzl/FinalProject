using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class Conversation : BaseEntity
{
    public string? DirectKey { get; set; }

    public static string DirectKeyFor(Guid userA, Guid userB)
    {
        var a = userA.ToString();
        var b = userB.ToString();
        return string.CompareOrdinal(a, b) <= 0 ? $"{a}:{b}" : $"{b}:{a}";
    }

    public Guid? GroupId { get; set; }
    public Group? Group { get; set; }

    public bool IsGroup => GroupId != null || IsCustomGroup;

    public string? Title { get; set; }
    public string? AvatarUrl { get; set; }
    public Guid? CreatedByUserId { get; set; }

    public bool IsCustomGroup => Title != null && GroupId == null && DirectKey == null;

    public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
