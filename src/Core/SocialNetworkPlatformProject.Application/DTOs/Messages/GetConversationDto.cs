namespace SocialNetworkPlatformProject.Application.DTOs.Messages;

public class GetConversationDto
{
    public Guid Id { get; set; }

    public Guid? GroupId { get; set; }
    public bool IsGroup { get; set; }
    public int MemberCount { get; set; }

    public bool IsCustomGroup { get; set; }
    public bool CanManage { get; set; }
    public bool IsMuted { get; set; }

    public Guid OtherUserId { get; set; }
    public string OtherUserName { get; set; } = string.Empty;
    public string? OtherUserAvatarUrl { get; set; }
    public bool IsOtherUserOnline { get; set; }

    public string? LastMessageText { get; set; }
    public DateTime? LastMessageAt { get; set; }
    public bool LastMessageIsMine { get; set; }
    public string? LastMessageSenderName { get; set; }
    public int UnreadCount { get; set; }
}
