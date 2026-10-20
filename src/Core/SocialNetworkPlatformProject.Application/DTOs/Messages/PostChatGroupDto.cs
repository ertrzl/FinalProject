using Microsoft.AspNetCore.Http;

namespace SocialNetworkPlatformProject.Application.DTOs.Messages;

// "Yeni grup" on the Messages page: a name, the friends to start with (the form field "memberIds" is repeated, one per
// friend) and optionally a picture. The person who makes it is in it and runs it.
public class PostChatGroupDto
{
    public string Title { get; set; } = string.Empty;
    public List<Guid> MemberIds { get; set; } = new();
    public IFormFile? Avatar { get; set; }
}
