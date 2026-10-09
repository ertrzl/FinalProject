namespace SocialNetworkPlatformProject.Application.Common;

// One e-mail to one person. The text version is always there; the HTML version is optional.
public record EmailMessage(string To, string Subject, string TextBody, string? HtmlBody = null);
