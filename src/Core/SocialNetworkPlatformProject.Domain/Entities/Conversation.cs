using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// messages.html conversation list + bottom-right chat dock (js/chat-data.js, js/chat-dock.js)
public class Conversation : BaseEntity
{
    public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
