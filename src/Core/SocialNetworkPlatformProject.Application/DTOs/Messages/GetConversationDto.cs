namespace SocialNetworkPlatformProject.Application.DTOs.Messages;

// messages.html conversation list + bottom-right chat dock (mirrors js/chat-data.js's conversation shape)
public class GetConversationDto
{
    public Guid Id { get; set; }

    // A group's chat: the list shows the group (OtherUserName / OtherUserAvatarUrl then hold the group's name and picture,
    // OtherUserId is empty and nobody's online state is shown). Direct conversations: GroupId is null.
    public Guid? GroupId { get; set; }
    public bool IsGroup { get; set; }
    public int MemberCount { get; set; }

    // A group chat made on the Messages page: it has no GroupId; OtherUserName / OtherUserAvatarUrl hold its name and picture.
    // CanManage: the viewer runs it (rename, change the picture, add and remove people).
    public bool IsCustomGroup { get; set; }
    public bool CanManage { get; set; }
    public bool IsMuted { get; set; } // the viewer muted the group: no toasts, and it is left out of the unread total

    public Guid OtherUserId { get; set; }
    public string OtherUserName { get; set; } = string.Empty;
    public string? OtherUserAvatarUrl { get; set; }
    public bool IsOtherUserOnline { get; set; }

    public string? LastMessageText { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public bool LastMessageIsMine { get; set; }
    public string? LastMessageSenderName { get; set; } // group chats: who wrote the last message ("Ayşe: ...")
    public int UnreadCount { get; set; }
}
