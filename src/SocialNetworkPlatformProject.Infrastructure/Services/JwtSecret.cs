using Microsoft.Extensions.Configuration;

namespace SocialNetworkPlatformProject.Infrastructure.Services;

// The key that signs every login token. It must not live in the repository (anyone with the code could then
// forge a token for any user), so appsettings.json leaves it empty and each machine provides its own:
// "dotnet user-secrets" while developing, an environment variable (Jwt__SecretKey) on a server.
// The app refuses to start without a proper one instead of silently running with something guessable.
public static class JwtSecret
{
    public const int MinLength = 32;

    public static string Read(IConfiguration configuration)
    {
        var key = configuration["Jwt:SecretKey"];

        if (string.IsNullOrWhiteSpace(key) || key.Length < MinLength)
        {
            throw new InvalidOperationException(
                $"Jwt:SecretKey is missing or shorter than {MinLength} characters. " +
                "Set it on this machine, e.g.:  dotnet user-secrets set \"Jwt:SecretKey\" \"<a long random string>\" " +
                "--project src/SocialNetworkPlatformProject   (on a server: the environment variable Jwt__SecretKey).");
        }

        return key;
    }
}
