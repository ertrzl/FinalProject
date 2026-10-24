using Microsoft.AspNetCore.Http;

namespace SocialNetworkPlatformProject.Application.DTOs.Messages;

public class PostMessageImageDto
{
    public Guid? ConversationId { get; set; }
    public Guid? ReceiverId { get; set; }
    public IFormFile? Image { get; set; }
    public string? Text { get; set; }
    public Guid? ReplyToMessageId { get; set; }
}
