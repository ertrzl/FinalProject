namespace SocialNetworkPlatformProject.Application.DTOs.Messages;

public class GetMessageDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsMine { get; set; }
    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; }
}
