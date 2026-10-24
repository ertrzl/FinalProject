namespace SocialNetworkPlatformProject.Application.DTOs.Messages;

public class GetMessageReactionDto
{
    public string Emoji { get; set; } = string.Empty;
    public int Count { get; set; }
    public bool ReactedByMe { get; set; }
}
