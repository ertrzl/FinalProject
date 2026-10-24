namespace SocialNetworkPlatformProject.Application.DTOs.Messages;

public class PostChatGroupMembersDto
{
    public List<Guid> UserIds { get; set; } = new();
}
