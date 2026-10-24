using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

public class Post : BaseEntity
{
    public Guid AuthorId { get; set; }
    public string? Text { get; set; }
    public PostPrivacy Privacy { get; set; } = PostPrivacy.Public;

    public bool IsShare { get; set; }
    public Guid? SharedPostId { get; set; }
    public Post? SharedPost { get; set; }

    public Guid? SharedEventId { get; set; }

    public Poll? Poll { get; set; }

    public Guid? GroupId { get; set; }
    public Group? Group { get; set; }

    public ICollection<PostMedia> Media { get; set; } = new List<PostMedia>();

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<PostLike> Likes { get; set; } = new List<PostLike>();
    public ICollection<SavedPost> SavedBy { get; set; } = new List<SavedPost>();
    public ICollection<PostHashtag> Hashtags { get; set; } = new List<PostHashtag>();
}
