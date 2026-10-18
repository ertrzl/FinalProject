namespace SocialNetworkPlatformProject.Application.DTOs.Messages;

// The emoji to leave on a message. The same emoji again, or none, takes the reaction back.
public class PutMessageReactionDto
{
    public string? Emoji { get; set; }
}
