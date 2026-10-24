using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;

namespace SocialNetworkPlatformProject.Infrastructure.Services;

public static class JwtSecret
{
    public const int MinLength = 32;

    private const string KeyFileName = "jwt-secret.key";

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
