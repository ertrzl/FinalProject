namespace SocialNetworkPlatformProject.Application.DTOs.Common;

// Somebody tagged with "@username" in a post or a comment. Only people who really exist are listed, so the page links
// exactly these and leaves any other "@something" as plain text.
public class GetMentionDto
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}
