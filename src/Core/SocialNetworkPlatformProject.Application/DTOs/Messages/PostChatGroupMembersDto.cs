namespace SocialNetworkPlatformProject.Application.DTOs.Messages;

// Adding friends to a group chat (people who run it).
public class PostChatGroupMembersDto
{
    public List<Guid> UserIds { get; set; } = new();
}
