namespace SocialNetworkPlatformProject.Application.DTOs.Messages;

// The message a message answers, shown above it as a quote. When the quoted message was taken back, IsDeleted is true and
// there is no text or picture.
public class GetMessageReplyDto
{
    public Guid MessageId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public bool IsMine { get; set; } // the quoted message is the viewer's own: the quote says "Sen"
    public string Type { get; set; } = "Text"; // Text / Image / Sticker
    public string? Text { get; set; } // the beginning of it
    public string? MediaUrl { get; set; }
    public bool IsDeleted { get; set; }
}
