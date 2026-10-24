namespace SocialNetworkPlatformProject.Application.Common;

public record EmailMessage(string To, string Subject, string TextBody, string? HtmlBody = null);
