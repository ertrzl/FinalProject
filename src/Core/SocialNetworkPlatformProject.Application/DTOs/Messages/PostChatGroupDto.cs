using Microsoft.AspNetCore.Http;

namespace SocialNetworkPlatformProject.Application.DTOs.Messages;

public class PostChatGroupDto
{
    public string Title { get; set; } = string.Empty;
    public List<Guid> MemberIds { get; set; } = new();
    public IFormFile? Avatar { get; set; }
}
