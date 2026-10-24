namespace SocialNetworkPlatformProject.Application.DTOs.Common;

public class GetMentionDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}
