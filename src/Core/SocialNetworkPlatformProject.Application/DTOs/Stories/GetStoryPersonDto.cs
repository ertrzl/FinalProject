namespace SocialNetworkPlatformProject.Application.DTOs.Stories;

// A person shown on a story: someone tagged in it, or the owner of the story it was added from.
public class GetStoryPersonDto
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}
