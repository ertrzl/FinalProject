namespace SocialNetworkPlatformProject.Application.Common;

// The emojis that can be left on a message. The list is the server's rule (the pages only show it): anything else is refused.
public static class MessageReactions
{
    public static readonly IReadOnlyList<string> Allowed = new[] { "❤️", "😂", "😮", "😢", "😡", "👍" };

    // How much of the message a quote (the "reply to" box) shows.
    public const int QuotePreviewLength = 120;
}
