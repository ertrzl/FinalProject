using SocialNetworkPlatformProject.Application.DTOs.Music;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// Looks songs up for stories (Infrastructure: Apple's free iTunes search). Only results with a playable preview are returned.
public interface IMusicSearchService
{
    Task<List<GetMusicTrackDto>> SearchAsync(string query);
}
