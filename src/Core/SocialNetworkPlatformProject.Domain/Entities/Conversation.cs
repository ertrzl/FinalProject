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

    // Set on a group's chat (one per group, unique): its participants are the group's current members, kept in step by
    // GroupService. A direct conversation has DirectKey and no GroupId; a group chat has GroupId and no DirectKey.
    // Deleting the group deletes its chat and the messages (cascade).
    public Guid? GroupId { get; set; }
    public Group? Group { get; set; }

    public bool IsGroup => GroupId != null || IsCustomGroup;

    // A group chat made from the Messages page ("Yeni grup"): friends picked by one person, with a name of its own and
    // optionally a picture. It has neither DirectKey nor GroupId. The participants are its members; the ones with
    // ConversationParticipant.IsAdmin run it (add and remove people, rename, change the picture).
    public string? Title { get; set; }
    public string? AvatarUrl { get; set; }
    public Guid? CreatedByUserId { get; set; }

    public bool IsCustomGroup => Title != null && GroupId == null && DirectKey == null;

    public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
