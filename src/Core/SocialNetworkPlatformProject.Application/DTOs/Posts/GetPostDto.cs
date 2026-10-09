using SocialNetworkPlatformProject.Application.DTOs.Common;

namespace SocialNetworkPlatformProject.Application.DTOs.Posts;

// Shape matches what home.html / profile.html's feed rendering expects
// (see the post card markup and js/app.js's publishPost()/toggleLike()).
public class GetPostDto
{
    public Guid Id { get; set; }

    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorAvatarUrl { get; set; }

    public string? Text { get; set; }
    public List<GetPostMediaDto> Media { get; set; } = new(); // photos or one video, in the author's order
    public List<GetMentionDto> Mentions { get; set; } = new(); // the people tagged with @username in Text
    public string Privacy { get; set; } = string.Empty; // "Public" / "FriendsOnly"

    // A post on a group's wall: who sees it follows the group, so the page shows no privacy label or choice for it.
    public bool IsGroupPost { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Hashtags { get; set; } = new();

    public int LikeCount { get; set; }                  // all reactions together
    public List<GetReactionCountDto> Reactions { get; set; } = new(); // the split, biggest first
    public string? MyReaction { get; set; }              // the viewer's own reaction; null = none
    public int CommentCount { get; set; }
    public int ShareCount { get; set; }                  // how many times this post was shared
    public bool IsLikedByCurrentUser { get; set; }       // any reaction

    // A share: IsShare is true and SharedPost is the original, or null when the viewer may not see it any more
    // (deleted, or no longer visible to them): the card then says so.
    public bool IsShare { get; set; }
    public GetSharedPostDto? SharedPost { get; set; }

    public GetPollDto? Poll { get; set; }
    public bool IsSavedByCurrentUser { get; set; }

    // The author, or (on a group post) an admin/moderator of that group. The server decides; the page only shows the button.
    public bool CanDelete { get; set; }

    // Only the author may change the text and privacy of a post.
    public bool CanEdit { get; set; }
}
