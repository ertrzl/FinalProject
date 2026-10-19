using SocialNetworkPlatformProject.Domain.Common;

namespace SocialNetworkPlatformProject.Domain.Entities;

// Somebody tagged with "@username" in the text of a post. The text stays the truth (that is what the post shows); these rows are a copy
// kept in step with it whenever the post is written or edited, so "posts I am tagged in" (profile "Etiketlendiklerim" tab) is one indexed
// query instead of a search through every text. Not the author tagging themselves. Gone with the post.
public class PostMention : BaseEntity
{
    public Guid PostId { get; set; }
    public Post? Post { get; set; }

    public Guid MentionedUserId { get; set; }
}
