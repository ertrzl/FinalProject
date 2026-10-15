using SocialNetworkPlatformProject.Application.Common;

namespace SocialNetworkPlatformProject.Application.DTOs.Comments;

// One page of a post's comments. Items are the page's top-level comments, each followed by all of its replies (oldest first);
// TotalCount / TotalPages / HasMore count the top-level comments, CommentCount every comment of the post (the number the
// post shows next to "yorum").
public class GetCommentPageDto : PagedResult<GetCommentDto>
{
    public int CommentCount { get; set; }
}
