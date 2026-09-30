using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

// F3/F4: composer + news feed (home.html, profile.html "Gönderiler" tab)
public class Post : BaseEntity
{
    public Guid AuthorId { get; set; }
    public string? Text { get; set; }
    public string? MediaUrl { get; set; }
    public PostMediaType MediaType { get; set; } = PostMediaType.Image;
    public PostPrivacy Privacy { get; set; } = PostPrivacy.Public;

    // Set when this post was made on a group's wall instead of the author's own feed.
    // Visibility then follows the group's privacy/membership, not Privacy above.
    public Guid? GroupId { get; set; }
    public Group? Group { get; set; }

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<PostLike> Likes { get; set; } = new List<PostLike>();
    public ICollection<SavedPost> SavedBy { get; set; } = new List<SavedPost>();
    public ICollection<PostHashtag> Hashtags { get; set; } = new List<PostHashtag>();
}
