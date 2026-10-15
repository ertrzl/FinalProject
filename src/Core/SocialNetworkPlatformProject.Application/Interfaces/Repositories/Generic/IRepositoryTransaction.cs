namespace SocialNetworkPlatformProject.Application.Interfaces.Repositories.Generic;

// One all-or-nothing step over several repositories (they share one database context per request). Leaving the
// `await using` block without CommitAsync undoes everything done inside it.
public interface IRepositoryTransaction : IAsyncDisposable
{
    Task CommitAsync();
}
