namespace SocialNetworkPlatformProject.Application.Common;

public static class MessageReactions
{
    public static readonly IReadOnlyList<string> Allowed = new[] { "❤️", "😂", "😮", "😢", "😡", "👍" };

    public const int QuotePreviewLength = 120;
}
