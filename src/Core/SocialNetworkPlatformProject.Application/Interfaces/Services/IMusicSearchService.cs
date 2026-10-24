using SocialNetworkPlatformProject.Application.DTOs.Music;

namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IMusicSearchService
{
    Task<List<GetMusicTrackDto>> SearchAsync(string query);
}
