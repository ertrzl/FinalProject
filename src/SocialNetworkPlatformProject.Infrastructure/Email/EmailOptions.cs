namespace SocialNetworkPlatformProject.Infrastructure.Email;

// The "Email" section of the settings. Without an SMTP host nothing is really sent (see ServiceRegistration).
public class EmailOptions
{
    public const string SectionName = "Email";

    // The sender people see. Gmail and most providers insist on the account's own address, so when this is empty
    // the SMTP user name is used.
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "SocialNet";

    public SmtpOptions Smtp { get; set; } = new();
}

public class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    // Encrypt the connection (STARTTLS on port 587, TLS from the first byte on 465). Turn off only for a local test server.
    public bool UseSsl { get; set; } = true;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);
}
