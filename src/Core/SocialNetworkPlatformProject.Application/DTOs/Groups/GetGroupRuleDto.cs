namespace SocialNetworkPlatformProject.Application.DTOs.Groups;

// group.html "Kurallar" tab, and the rules shown before joining (groups.html / group.html).
public class GetGroupRuleDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Text { get; set; }
}
