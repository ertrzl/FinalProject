using SocialNetworkPlatformProject.Domain.Common;
using SocialNetworkPlatformProject.Domain.Enums;

namespace SocialNetworkPlatformProject.Domain.Entities;

// F3/F4: composer + news feed (home.html, profile.html "Gönderiler" tab)
public class Post : BaseEntity
{
    public Guid AuthorId { get; set; }
    public string? Text { get; set; }
    public PostPrivacy Privacy { get; set; } = PostPrivacy.Public;

    // A share ("Gönderiyi paylaş"): this post shows another post (SharedPostId), with an optional text of its own on
    // top. IsShare stays true when the original is deleted (SharedPostId is then null: the card says it is gone).
    public bool IsShare { get; set; }
    public Guid? SharedPostId { get; set; }
    public Post? SharedPost { get; set; }

    // An event shared to the feed ("Gönderide paylaş" on event.html): this post shows the event as a card. A soft
    // reference (no foreign key): when the event is deleted the post stays and its card says the event is gone.
    public Guid? SharedEventId { get; set; }

    public Poll? Poll { get; set; }

    // Set when this post was made on a group's wall instead of the author's own feed.
    // Visibility then follows the group's privacy/membership, not Privacy above.
    public Guid? GroupId { get; set; }
    public Group? Group { get; set; }

    // Photos (up to PostLimits.MaxImages) or one video, in the order the author chose; empty for a text-only post.
    public ICollection<PostMedia> Media { get; set; } = new List<PostMedia>();

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<PostLike> Likes { get; set; } = new List<PostLike>();
    public ICollection<SavedPost> SavedBy { get; set; } = new List<SavedPost>();
    public ICollection<PostHashtag> Hashtags { get; set; } = new List<PostHashtag>();
}
