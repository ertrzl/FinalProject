using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;

namespace SocialNetworkPlatformProject.Infrastructure.Services;

// The key that signs every login token. It is the server's own secret: users never see it or type it. It must not live
// in the repository (anyone with the code could then forge a token for any user), so appsettings.json leaves it empty
// and the app gets it from, in this order:
//   1. configuration: the environment variable Jwt__SecretKey on a server, or "dotnet user-secrets" on a developer's
//      machine;
//   2. otherwise a key file the app creates for itself the first time it runs (random, never in git, kept next to the
//      app so tokens stay valid across restarts).
// So anybody who downloads the project can simply start it, and no two machines share a key.
public static class JwtSecret
{
    public const int MinLength = 32;

    private const string KeyFileName = "jwt-secret.key";

    // Call once at start-up, before anything reads the key. A key that is configured is left alone (Read checks that
    // it is long enough); a key that is simply not configured is loaded from, or created in, the data directory.
    public static void EnsureConfigured(IConfiguration configuration, string dataDirectory)
    {
        if (!string.IsNullOrWhiteSpace(configuration["Jwt:SecretKey"]))
            return;

        configuration["Jwt:SecretKey"] = LoadOrCreateKeyFile(Path.Combine(dataDirectory, KeyFileName));
    }

    public static string Read(IConfiguration configuration)
    {
        var key = configuration["Jwt:SecretKey"];

        if (string.IsNullOrWhiteSpace(key) || key.Length < MinLength)
        {
            throw new InvalidOperationException(
                $"Jwt:SecretKey is missing or shorter than {MinLength} characters. Leave it out of the configuration and the app " +
                "creates its own key, or set a longer one (dotnet user-secrets, or the environment variable Jwt__SecretKey).");
        }

        return key;
    }

    private static string LoadOrCreateKeyFile(string path)
    {
        var existing = TryReadKey(path);
        if (existing != null)
            return existing;

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        // Written to a temporary file and moved into place: the move fails if the file is already there, so when two
        // instances start for the very first time together, one key wins and both use it (no half-written file).
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var temporary = $"{path}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(temporary, key);

        try
        {
            File.Move(temporary, path);
            return key;
        }
        catch (IOException)
        {
            File.Delete(temporary);
            return TryReadKey(path)
                ?? throw new InvalidOperationException($"The key file '{path}' exists but is not a valid key. Delete it and start the app again.");
        }
    }

    private static string? TryReadKey(string path)
    {
        if (!File.Exists(path))
            return null;

        var key = File.ReadAllText(path).Trim();
        return key.Length >= MinLength ? key : null;
    }
}
