namespace SocialNetworkPlatformProject.Application.DTOs.Messages;

// One emoji under a message: how many people left it and whether the viewer is one of them (its pill is highlighted).
public class GetMessageReactionDto
{
    public string Emoji { get; set; } = string.Empty;
    public int Count { get; set; }
    public bool ReactedByMe { get; set; }
}
