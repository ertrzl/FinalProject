using Microsoft.AspNetCore.Http;

namespace SocialNetworkPlatformProject.Application.DTOs.Messages;

// Changing a group chat's name and picture (people who run it). Left out = unchanged; RemoveAvatar takes the picture away.
public class PutChatGroupDto
{
    public string? Title { get; set; }
    public IFormFile? Avatar { get; set; }
    public bool RemoveAvatar { get; set; }
}
