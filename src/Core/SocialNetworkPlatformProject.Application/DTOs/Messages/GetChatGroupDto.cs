namespace SocialNetworkPlatformProject.Application.DTOs.Messages;

// The "Grup bilgisi" window of a group chat made on the Messages page: its name, picture and people.
public class GetChatGroupDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int MemberCount { get; set; }
    public bool CanManage { get; set; } // the viewer runs it: rename, change the picture, add and remove people
    public bool IsMuted { get; set; }
    public int MaxMembers { get; set; }
    public List<GetChatGroupMemberDto> Members { get; set; } = new();
}

public class GetChatGroupMemberDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool IsAdmin { get; set; }
    public bool IsMe { get; set; }
}
