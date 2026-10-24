namespace SocialNetworkPlatformProject.Extensions;

public static class LinkBaseUrlExtensions
{
    public static string? LinkBaseUrl(this HttpRequest request, IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration["App:PublicBaseUrl"];
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.Trim().TrimEnd('/');

        return environment.IsDevelopment() ? $"{request.Scheme}://{request.Host}" : null;
    }
}
