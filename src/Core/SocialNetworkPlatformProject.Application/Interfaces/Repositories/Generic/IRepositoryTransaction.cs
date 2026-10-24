namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;

public interface IRepositoryTransaction : IAsyncDisposable
{
    Task CommitAsync();
}
