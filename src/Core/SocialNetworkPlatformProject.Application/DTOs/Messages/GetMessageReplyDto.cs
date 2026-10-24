namespace SocialNetworkPlatformProject.Application.DTOs.Messages;

public class GetMessageReplyDto
{
    public Guid MessageId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public bool IsMine { get; set; }
    public string Type { get; set; } = "Text";
    public string? Text { get; set; }
    public string? MediaUrl { get; set; }
    public bool IsDeleted { get; set; }
}
