using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

// F3/F4: composer + news feed (home.html, profile.html "Gönderiler" tab)
public class Post : BaseEntity
{
    public Guid AuthorId { get; set; }
    public string? Text { get; set; }
    public string? ImageUrl { get; set; }
    public PostPrivacy Privacy { get; set; } = PostPrivacy.Public;

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<PostLike> Likes { get; set; } = new List<PostLike>();
    public ICollection<SavedPost> SavedBy { get; set; } = new List<SavedPost>();
}
