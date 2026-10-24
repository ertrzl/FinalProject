namespace SocialNetworkPlatformProject.Application.DTOs.Comments;

public class GetCommentPageDto : PagedResult<GetCommentDto>
{
    public int CommentCount { get; set; }
}
