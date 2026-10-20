namespace SocialNetworkPlatformProject.Application.DTOs.Groups;

// group.html's admin-only "Kuralları Düzenle" modal: the whole list, in order. It replaces the rules the group had;
// an empty list removes them all (joining then asks for nothing).
public class PutGroupRulesDto
{
    public List<PutGroupRuleItemDto> Rules { get; set; } = new();
}

public class PutGroupRuleItemDto
{
    public string Title { get; set; } = string.Empty;
    public string? Text { get; set; }
}
