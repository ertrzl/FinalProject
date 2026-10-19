using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SocialNetworkPlatformProject.Application.Common;
using SocialNetworkPlatformProject.Application.DTOs.Music;
using SocialNetworkPlatformProject.Application.Exceptions;
using SocialNetworkPlatformProject.Application.Interfaces.Services;

namespace SocialNetworkPlatformProject.Infrastructure.Services;

public class ItunesMusicSearchService : IMusicSearchService
{
    private const int MaxResults = 15;

    private readonly HttpClient _http;
    private readonly ILogger<ItunesMusicSearchService> _logger;

    public ItunesMusicSearchService(HttpClient http, ILogger<ItunesMusicSearchService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<List<GetMusicTrackDto>> SearchAsync(string query)
    {
        try
        {
            var url = $"https://itunes.apple.com/search?media=music&entity=song&limit={MaxResults}&country=TR&term={Uri.EscapeDataString(query)}";
            var answer = await _http.GetFromJsonAsync<ItunesAnswer>(url);

            return (answer?.Results ?? new List<ItunesTrack>())
                .Where(t => !string.IsNullOrWhiteSpace(t.TrackName) && MusicLinks.IsAllowedAudio(t.PreviewUrl))
                .Select(t => new GetMusicTrackDto
                {
                    Title = t.TrackName!,
                    Artist = t.ArtistName ?? string.Empty,
                    PreviewUrl = t.PreviewUrl!,
                    // The store sends a 100 px cover; a slightly bigger one is as cheap and looks sharp in the viewer.
                    ArtworkUrl = MusicLinks.IsAllowedCover(t.ArtworkUrl100) ? t.ArtworkUrl100!.Replace("100x100", "200x200") : null
                })
                .ToList();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            _logger.LogWarning(ex, "Music search failed for a query of {Length} characters", query.Length);
            throw new BadRequestException(Loc.T("music.unavailable"));
        }
    }

    private class ItunesAnswer
    {
        [JsonPropertyName("results")] public List<ItunesTrack>? Results { get; set; }
    }

    private class ItunesTrack
    {
        [JsonPropertyName("trackName")] public string? TrackName { get; set; }
        [JsonPropertyName("artistName")] public string? ArtistName { get; set; }
        [JsonPropertyName("previewUrl")] public string? PreviewUrl { get; set; }
        [JsonPropertyName("artworkUrl100")] public string? ArtworkUrl100 { get; set; }
    }
}
