using Microsoft.AspNetCore.Http;

namespace SocialNetworkPlatformProject.Infrastructure.Services;

internal static class FileSignatures
{
    public static async Task<bool> MatchesAsync(IFormFile file, string extension)
    {
        var header = new byte[16];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length));
        return Matches(extension.ToLowerInvariant(), header.AsSpan(0, read));
    }

    private static bool Matches(string extension, ReadOnlySpan<byte> header)
    {
        return extension switch
        {
            ".jpg" or ".jpeg" => header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => header.Length >= 8 && header[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".gif" => header.Length >= 6 && (header[..6].SequenceEqual("GIF87a"u8) || header[..6].SequenceEqual("GIF89a"u8)),
            ".webp" => header.Length >= 12 && header[..4].SequenceEqual("RIFF"u8) && header[8..12].SequenceEqual("WEBP"u8),
            ".mp4" or ".mov" => header.Length >= 12 && IsMovieBox(header[4..8]),
            ".webm" => header.Length >= 4 && header[0] == 0x1A && header[1] == 0x45 && header[2] == 0xDF && header[3] == 0xA3,
            _ => false
        };
    }

    private static bool IsMovieBox(ReadOnlySpan<byte> name) =>
        name.SequenceEqual("ftyp"u8) || name.SequenceEqual("moov"u8) || name.SequenceEqual("mdat"u8)
        || name.SequenceEqual("wide"u8) || name.SequenceEqual("free"u8);
}
