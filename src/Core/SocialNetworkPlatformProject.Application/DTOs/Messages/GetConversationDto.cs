namespace SocialNetworkPlatformProject.Application.DTOs.Messages;

// messages.html conversation list + bottom-right chat dock (mirrors js/chat-data.js's conversation shape)
public class GetConversationDto
{
    public Guid Id { get; set; }
    public Guid OtherUserId { get; set; }
    public string OtherUserName { get; set; } = string.Empty;
    public string? OtherUserAvatarUrl { get; set; }
    public bool IsOtherUserOnline { get; set; }

    public string? LastMessageText { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public bool LastMessageIsMine { get; set; }
    public int UnreadCount { get; set; }
}
