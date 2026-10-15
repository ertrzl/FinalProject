namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

// Who is calling: the browser and the address of the current request. Implemented in the web project, so Application and
// Persistence never touch HttpContext (the same reason services take an explicit currentUserId).
public interface IClientInfo
{
    string? UserAgent { get; }

    string? IpAddress { get; }
}
