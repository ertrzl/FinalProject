namespace SocialNetworkPlatformProject.Application.Interfaces.Services;

public interface IUploadAccessService
{
    Task<bool> CanViewAsync(Guid viewerId, string folder, string url);
}
