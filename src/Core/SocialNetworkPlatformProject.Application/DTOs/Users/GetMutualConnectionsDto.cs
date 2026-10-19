namespace SocialNetworkPlatformProject.Application.DTOs.Users;

// "12 ortak arkadaş" and "3 ortak grup" on somebody else's profile (profile.html). The counts are the real totals; the lists hold the first 100 of each.
public class GetMutualConnectionsDto
{
    public int FriendCount { get; set; }
    public List<GetMutualFriendDto> Friends { get; set; } = new();

    public int GroupCount { get; set; }
    public List<GetMutualGroupDto> Groups { get; set; } = new();
}

public class GetMutualFriendDto
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}

public class GetMutualGroupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public int MemberCount { get; set; }
}
