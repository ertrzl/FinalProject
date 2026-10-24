namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IClientInfo
{
    string? UserAgent { get; }

    string? IpAddress { get; }
}
