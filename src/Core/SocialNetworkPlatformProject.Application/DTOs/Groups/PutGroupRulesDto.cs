namespace SocialNetworkPlatformProject.Application.DTOs.Groups;

public class PutGroupRulesDto
{
    public List<PutGroupRuleItemDto> Rules { get; set; } = new();
}

public class PutGroupRuleItemDto
{
    public string Title { get; set; } = string.Empty;
    public string? Text { get; set; }
}
