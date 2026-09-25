using Microsoft.AspNetCore.Http;

namespace SocialNetworkPlatformProject.Application.DTOs.Messages;

// Photo message (multipart form, so it can't go through the SignalR hub). Text is an optional caption.
public class PostMessageImageDto
{
    public Guid? ConversationId { get; set; }
    public Guid? ReceiverId { get; set; }
    public IFormFile? Image { get; set; }
    public string? Text { get; set; }
}
