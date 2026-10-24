namespace SocialNetworkPlatformProject.Application.DTOs.Groups;

public class GetGroupRuleDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Text { get; set; }
}
