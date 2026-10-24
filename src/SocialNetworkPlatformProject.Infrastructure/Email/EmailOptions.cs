namespace SocialNetworkPlatformProject.Infrastructure.Email;

public class EmailOptions
{
    public const string SectionName = "Email";

    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "Stoop";

    public SmtpOptions Smtp { get; set; } = new();
}

public class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public bool UseSsl { get; set; } = true;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);
}
