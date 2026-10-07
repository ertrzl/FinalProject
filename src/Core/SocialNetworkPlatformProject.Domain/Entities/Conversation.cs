using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// messages.html conversation list + bottom-right chat dock (js/chat-data.js, js/chat-dock.js)
public class Conversation : BaseEntity
{
    // Names the two people of a direct conversation, whoever opened it ("smaller-id:larger-id"). It is unique, so two
    // people who message each other for the first time at the same moment still end up in one conversation.
    public string? DirectKey { get; set; }

    public static string DirectKeyFor(Guid userA, Guid userB)
    {
        var a = userA.ToString();
        var b = userB.ToString();
        return string.CompareOrdinal(a, b) <= 0 ? $"{a}:{b}" : $"{b}:{a}";
    }

    public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
